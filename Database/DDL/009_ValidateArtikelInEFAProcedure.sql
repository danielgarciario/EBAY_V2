create procedure [ebay].[CheckedImportAgainstEFA]
as
BEGIN
/* COMPROBAR QUE LO QUE HEMOS IMPORTADO ESTA EN LN
Compureba los nuevos registros de InventorySellableEfaValidation (los que estan pending)
1. Descompone SKU en item+qtun
2. Comprueba que item existe, si qtun = cuni => 1, si no tiene que existir URF != null
- Lo que encuentra lo devuelve en [InventorySellableEfaValidation] pudes hacer un inner join contra InventorySellableSnapshot para tener item y qtun.
*/
declare @ctrl table (item nvarchar(47) not null,qtun nvarchar(3) not null, primary key (item,qtun));
declare @origen table (Id bigint not null, [SellableSnapshotId] bigint not null, item nvarchar(47) null, qtun nvarchar(3) null)
declare @ahora datetime2;

insert into @origen(Id,SellableSnapshotId, item, qtun)
SELECT  isv.[Id]
      ,isv.[SellableSnapshotId]
      ,ln.item
      ,ln.qtun

  FROM [EBAY].[ebay].[InventorySellableEfaValidation] isv
  LEFT JOIN [ebay].InventorySellableSnapshot iss on iss.Id = isv.SellableSnapshotId
  CROSS APPLY [ebay].[SKU2ItemQtun](iss.SellableSku) ln
  where isv.ValidationStatus = 'Pending';

insert into @ctrl(item,qtun)
SELECT item,qtun
from @origen
where item is not null and qtun is not null
group by item,qtun;

set @ahora = SYSDATETIME();

with salctrl as (
select c.*, aa.cuni, urf.conv,aa.kitm,
case when aa.cuni = c.qtun then 1 when conv is not null then 1 else 0 end Ok
from @ctrl c
left join [100].tcibd001 aa on aa.item = c.item
left join [100].tcibd003 urf on
        urf.item = c.item
        and urf.basu = aa.cuni
        and urf.unit = c.qtun
        and urf.appr = 1)


update isv SET
    ParsedEfaItem = o.item,
    ParsedEfaSalesUnit = o.qtun,
    ValidationStatus = CASE WHEN sc.Ok=1 THEN 'Valid' else 'Failed' end,
    EfaItemFound = 1,
    EfaSalesUnitFound = 1,
    EfaItemIsActive = CASE WHEN sc.Ok = 1 then 1 ELSE 0 END,
    ValidatedAtLocal = @ahora

from [ebay].[InventorySellableEfaValidation] isv
inner join @origen o on o.Id = isv.Id
left join salctrl sc on sc.item = o.item and sc.qtun = o.qtun
END
