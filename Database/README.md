# Base de datos EBAY

Scripts iniciales para crear el esquema `ebay` en la base de datos `EBAY`.

Orden de ejecución:

1. `DDL/001_CreateSchema.sql`
2. `DDL/002_CreateTypes.sql`
3. `DDL/003_CreateInventoryTables.sql`
4. `DDL/004_CreateInventoryImportTableTypes.sql`
5. `DDL/005_CreateInventoryImportProcedures.sql`
6. `DDL/006_CreateInventoryViews.sql`
7. `DDL/007_CreateMaintenanceProcedures.sql`
8. `DDL/008_CreateSqlAgentJobs.sql`
9. `DDL/009_ValidateArtikelInEFAProcedure.sql`
10. `DDL/010_CreateOrderTypes.sql`
11. `DDL/011_CreateOrderTables.sql`
12. `DDL/012_CreateOrderImportTableTypes.sql`
13. `DDL/013_CreateOrderImportProcedure.sql`
14. `DDL/014_CreateOrderViews.sql`
15. `DDL/015_CreateOrderMaintenanceProcedure.sql`
16. `DDL/016_CreateOrderCleanupSqlAgentJob.sql`

Notas:

1. La base de datos `EBAY` ya existe en el servidor de Continum.
2. Los scripts crean objetos dentro del esquema `[ebay]`.
3. Los tipos comunes se definen como User Defined Data Types bajo `[ebay]`.
4. Los scripts de tablas son una base inicial, no un sistema de migraciones.
5. Las fechas se guardan como hora local Europe/Berlin.
6. La importación desde C# está pensada para DataTables enviados como table-valued parameters.
7. El job de SQL Server Agent borra diariamente los lotes de inventario cuya retención haya caducado.
8. El payload JSON original de los pedidos se conserva 30 días; los pedidos normalizados se conservan 6 meses.
9. Si se ejecuta en una instancia sin SQL Server Agent, no ejecutar `008_CreateSqlAgentJobs.sql` ni `016_CreateOrderCleanupSqlAgentJob.sql`.
10. Codex no valida estos scripts contra ninguna base de datos en este proyecto; la validación y ejecución las hace el usuario.
