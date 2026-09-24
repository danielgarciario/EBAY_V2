USE [msdb];
GO

DECLARE @JobName sysname = N'EBAY - Cleanup order history';
DECLARE @ScheduleName sysname = N'EBAY - Daily cleanup order history';
DECLARE @JobId uniqueidentifier;
DECLARE @ScheduleId int;

SELECT @JobId = [job_id]
FROM [msdb].[dbo].[sysjobs]
WHERE [name] = @JobName;

IF @JobId IS NULL
BEGIN
    EXEC [msdb].[dbo].[sp_add_job]
        @job_name = @JobName,
        @enabled = 1,
        @description = N'Deletes EBAY order data after six months and payloads after 30 days.';

    SELECT @JobId = [job_id]
    FROM [msdb].[dbo].[sysjobs]
    WHERE [name] = @JobName;
END;

IF NOT EXISTS (SELECT 1 FROM [msdb].[dbo].[sysjobsteps] WHERE [job_id] = @JobId AND [step_name] = N'Run cleanup procedure')
BEGIN
    EXEC [msdb].[dbo].[sp_add_jobstep]
        @job_name = @JobName,
        @step_name = N'Run cleanup procedure',
        @subsystem = N'TSQL',
        @database_name = N'EBAY',
        @command = N'DECLARE @ClearedSourceContents int;
DECLARE @DeletedOrders int;
DECLARE @DeletedImportBatches int;
EXEC [ebay].[CleanupOrderHistory]
    @ClearedSourceContents = @ClearedSourceContents OUTPUT,
    @DeletedOrders = @DeletedOrders OUTPUT,
    @DeletedImportBatches = @DeletedImportBatches OUTPUT;';
END;

SELECT @ScheduleId = [schedule_id]
FROM [msdb].[dbo].[sysschedules]
WHERE [name] = @ScheduleName;

IF @ScheduleId IS NULL
BEGIN
    EXEC [msdb].[dbo].[sp_add_schedule]
        @schedule_name = @ScheduleName,
        @enabled = 1,
        @freq_type = 4,
        @freq_interval = 1,
        @freq_subday_type = 1,
        @freq_subday_interval = 0,
        @active_start_time = 023000;

    SELECT @ScheduleId = [schedule_id]
    FROM [msdb].[dbo].[sysschedules]
    WHERE [name] = @ScheduleName;
END;

IF NOT EXISTS (SELECT 1 FROM [msdb].[dbo].[sysjobschedules] WHERE [job_id] = @JobId AND [schedule_id] = @ScheduleId)
BEGIN
    EXEC [msdb].[dbo].[sp_attach_schedule]
        @job_name = @JobName,
        @schedule_name = @ScheduleName;
END;

IF NOT EXISTS (SELECT 1 FROM [msdb].[dbo].[sysjobservers] WHERE [job_id] = @JobId)
BEGIN
    EXEC [msdb].[dbo].[sp_add_jobserver]
        @job_name = @JobName;
END;
GO
