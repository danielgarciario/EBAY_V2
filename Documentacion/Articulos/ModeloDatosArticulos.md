# Modelo de datos para artículos

## Objetivo del modelo

Guardar el inventario activo recibido desde eBay, conservar evidencia de cada importación y preparar la comparación contra EfA.

El modelo debe cubrir dos formas de artículo eBay:

1. Listing simple: una fila `SKUDetails` sin `Variations`.
2. Listing con variaciones: una fila `SKUDetails` padre y varias filas `Variation`.

## Principios

1. Guardar cada importación como un lote independiente.
2. Guardar el ZIP original descargado desde eBay en la base de datos.
3. Conservar el histórico durante 60 días.
4. No usar `SKU` como clave única global.
5. Tratar cada variante como unidad vendible.
6. Separar el dato reportado por eBay de la decisión interna de compatibilidad con EfA.
7. Separar snapshots históricos de la proyección actual que usará el proceso de stock.
8. No crear una tabla de mapeo inicial entre eBay y EfA.
9. Validar la compatibilidad descomponiendo el SKU eBay en `item` y `qtun`, y buscando esa combinación en EfA.
10. Guardar fechas en hora local Europe/Berlin, sin sufijo `Utc`.
11. Preparar la importación desde C# con table-valued parameters y stored procedures idempotentes basados en `MERGE`.

## Tabla: EbayInventoryImportBatch

Representa una importación del informe de inventario activo.

Campos propuestos:

| Campo | Tipo sugerido | Comentario |
| --- | --- | --- |
| Id | bigint identity | Clave interna |
| Source | nvarchar(64) | Por ejemplo `SellFeedApi` |
| FeedType | nvarchar(64) | Por ejemplo `LMS_ACTIVE_INVENTORY_REPORT` |
| TaskId | nvarchar(128) | Task id devuelto por eBay |
| Status | nvarchar(32) | Estado interno de importación |
| EbayAck | nvarchar(32) | Valor de `Ack`, por ejemplo `Success` |
| SourceFileName | nvarchar(260) | Nombre del ZIP original |
| SourceContent | varbinary(max) | Contenido binario del ZIP original descargado desde eBay |
| SourceFileSha256 | char(64) | Hash del ZIP original para auditoría |
| StartedAtLocal | datetime2 | Inicio del proceso en hora local Europe/Berlin |
| CompletedAtLocal | datetime2 | Fin del proceso en hora local Europe/Berlin |
| ImportedAtLocal | datetime2 | Momento de carga en base de datos en hora local Europe/Berlin |
| RetentionUntilLocal | datetime2 | Fecha local hasta la que se conserva el batch, normalmente 60 días |
| SkuDetailsCount | int | Número de nodos `SKUDetails` |
| VariationCount | int | Número de nodos `Variation` |
| ErrorMessage | nvarchar(max) | Error técnico si falla |

## Tipos SQL definidos

Los tipos comunes se definen como User Defined Data Types en el esquema `[ebay]`.

Tipos iniciales:

1. `[ebay].[dinero]`: `decimal(18,4)`.
2. `[ebay].[cantidad]`: `int`.
3. `[ebay].[fechaLocal]`: `datetime2(7)`.
4. `[ebay].[SKU]`: `nvarchar(64)`.
5. `[ebay].[eBayItemId]`: `nvarchar(32)`.
6. `[ebay].[taskId]`: `nvarchar(128)`.
7. `[ebay].[feedType]`: `nvarchar(64)`.
8. `[ebay].[sourceName]`: `nvarchar(64)`.
9. `[ebay].[statusCode]`: `nvarchar(32)`.
10. `[ebay].[fileName]`: `nvarchar(260)`.
11. `[ebay].[sha256]`: `char(64)`.
12. `[ebay].[currencyCode]`: `char(3)`.
13. `[ebay].[efaItem]`: `nvarchar(64)`.
14. `[ebay].[efaSalesUnit]`: `nvarchar(16)`.
15. `[ebay].[validationStatus]`: `nvarchar(32)`.
16. `[ebay].[issueType]`: `nvarchar(64)`.
17. `[ebay].[severity]`: `nvarchar(16)`.
18. `[ebay].[variationSpecificName]`: `nvarchar(128)`.
19. `[ebay].[variationSpecificValue]`: `nvarchar(256)`.

Los DataTables enviados desde C# se representan con User Defined Table Types documentados en [Importación idempotente de artículos](ImportacionIdempotente.md).

## Tabla: EbayInventoryListingSnapshot

Representa cada `SKUDetails` recibido en una importación.

Campos propuestos:

| Campo | Tipo sugerido | Comentario |
| --- | --- | --- |
| Id | bigint identity | Clave interna |
| ImportBatchId | bigint | FK a `EbayInventoryImportBatch` |
| EbayItemId | nvarchar(32) | `ItemID` de eBay |
| ParentSku | nvarchar(64) | `SKUDetails.SKU` |
| ReportedParentQuantity | int | `SKUDetails.Quantity` |
| HasVariations | bit | Si existe `Variations` |
| ParentPriceAmount | decimal(18,4) null | Solo para listings simples |
| ParentPriceCurrency | char(3) null | Por ejemplo `EUR` |

Índice o restricción recomendada:

1. Único por `ImportBatchId + EbayItemId`.

## Tabla: EbayInventorySellableSnapshot

Representa la unidad vendible normalizada.

Para listings simples habrá una fila por `SKUDetails`.

Para listings con variaciones habrá una fila por cada `Variation`.

Campos propuestos:

| Campo | Tipo sugerido | Comentario |
| --- | --- | --- |
| Id | bigint identity | Clave interna |
| ImportBatchId | bigint | FK a `EbayInventoryImportBatch` |
| ListingSnapshotId | bigint | FK a `EbayInventoryListingSnapshot` |
| EbayItemId | nvarchar(32) | Copia útil para consultas |
| ParentSku | nvarchar(64) | SKU padre del listing |
| SellableSku | nvarchar(64) | SKU simple o SKU de variante |
| IsVariation | bit | Distingue simple de variante |
| ReportedQuantity | int | Cantidad reportada por eBay para esa unidad vendible |
| PriceAmount | decimal(18,4) | Precio de la unidad vendible |
| PriceCurrency | char(3) | Moneda |

Índice o restricción recomendada:

1. Único por `ImportBatchId + EbayItemId + SellableSku`.

En el fichero analizado esa clave no tiene duplicados.

## Tabla: EbayInventoryVariationSpecificSnapshot

Representa cada `NameValueList` de una variante.

Campos propuestos:

| Campo | Tipo sugerido | Comentario |
| --- | --- | --- |
| Id | bigint identity | Clave interna |
| SellableSnapshotId | bigint | FK a `EbayInventorySellableSnapshot` |
| Name | nvarchar(128) | Nombre de la característica |
| Value | nvarchar(256) | Valor de la característica |
| SortOrder | int | Orden dentro del XML |

Índice recomendado:

1. `SellableSnapshotId`

## Validación contra EfA

No habrá tabla de mapeo inicial entre eBay y EfA.

La compatibilidad se validará directamente a partir del `SellableSku` de eBay.

En EfA el concepto equivalente al SKU eBay se forma con dos campos:

1. `item`: número de artículo.
2. `qtun`: unidad de venta, por ejemplo `St`, `Pal` o `m`.

El SKU de eBay sigue el patrón:

`{item}.{qtun}`

Ejemplos:

1. `XXXXX.St`
2. `XXXXX.Pal`

No existe actualmente una tabla lookup específica para traducir este enlace. La primera estrategia de mapeo será derivar `item` y `qtun` desde el `SellableSku` de eBay.

`qtun` lo define EfA. eBay no tiene una lista propia de unidades válida para este proyecto. La validación de `qtun` consiste en comprobar que la unidad extraída del SKU existe y es válida en EfA para el artículo indicado.

Flujo de validación propuesto:

1. Tomar `SellableSku`.
2. Separar el SKU por el último punto: parte izquierda como `item`, parte derecha como `qtun`.
3. Buscar en EfA el artículo `item`.
4. Comprobar que `qtun` es una unidad de venta válida para ese artículo en EfA.
5. Si no se encuentra la combinación `item + qtun`, registrar una incidencia.

Campos derivados recomendados en `EbayInventorySellableSnapshot` o en una vista de validación:

1. `ParsedEfaItem`
2. `ParsedEfaSalesUnit`
3. `EfaValidationStatus`
4. `EfaValidatedAtLocal`

Los SKUs repetidos se consideran incidencias en principio. Falta comprobar si todos los casos detectados son errores reales o si existen excepciones de negocio.

## Tabla: InventorySellableEfaValidation

Representa el resultado de validar una unidad vendible de eBay contra EfA.

No es una tabla de mapeo manual. Su función es guardar el resultado técnico de separar `SellableSku` en `item + qtun` y comprobar esa combinación en EfA.

Campos propuestos:

| Campo | Tipo sugerido | Comentario |
| --- | --- | --- |
| Id | bigint identity | Clave interna |
| SellableSnapshotId | bigint | FK a `InventorySellableSnapshot` |
| ParsedEfaItem | nvarchar(64) null | `item` extraído del SKU eBay |
| ParsedEfaSalesUnit | nvarchar(16) null | `qtun` extraído del SKU eBay |
| ValidationStatus | nvarchar(32) | Estado de validación |
| EfaItemFound | bit null | Si el artículo existe en EfA |
| EfaSalesUnitFound | bit null | Si la unidad de venta existe para el artículo en EfA |
| EfaItemIsActive | bit null | Si el artículo está activo en EfA |
| ValidatedAtLocal | datetime2 null | Fecha de validación en hora local Europe/Berlin |
| Message | nvarchar(max) null | Detalle técnico o explicación |

Estados iniciales:

1. `Pending`
2. `Valid`
3. `InvalidSkuFormat`
4. `MissingInEfa`
5. `MissingSalesUnitInEfa`
6. `InactiveInEfa`
7. `Failed`

## Tabla: EbayArticleCompatibilityIssue

Representa problemas detectados al comparar eBay con EfA.

Campos propuestos:

| Campo | Tipo sugerido | Comentario |
| --- | --- | --- |
| Id | bigint identity | Clave interna |
| ImportBatchId | bigint | Importación donde se detectó |
| EbayItemId | nvarchar(32) | Listing eBay |
| SellableSku | nvarchar(64) | SKU afectado |
| ParsedEfaItem | nvarchar(64) null | `item` extraído del SKU eBay |
| ParsedEfaSalesUnit | nvarchar(16) null | `qtun` extraído del SKU eBay |
| IssueType | nvarchar(64) | Tipo de problema |
| Severity | nvarchar(16) | `Info`, `Warning`, `Error` |
| Message | nvarchar(max) | Detalle interno |
| DetectedAtLocal | datetime2 | Fecha de detección en hora local Europe/Berlin |
| ResolvedAtLocal | datetime2 null | Fecha de resolución si aplica en hora local Europe/Berlin |

Tipos iniciales de problema:

1. `MissingInEfa`
2. `DuplicateSkuInEbay`
3. `MissingSalesUnitInEfa`
4. `InactiveInEfa`
5. `InvalidSkuFormat`
6. `InvalidEfaSalesUnit`
7. `BundleRequiresManualReview`
8. `StockUpdateBlocked`

## Vista o proyección: inventario actual de eBay

Para trabajar con el estado actual se puede crear una vista o tabla materializada basada en la última importación correcta.

Nombre posible:

1. `vwEbayCurrentSellableInventory`

Contenido esperado:

1. `EbayItemId`
2. `ParentSku`
3. `SellableSku`
4. `IsVariation`
5. `ReportedQuantity`
6. `PriceAmount`
7. `PriceCurrency`
8. `ImportBatchId`
9. `ImportedAtLocal`

Para la primera fase, una vista sobre el último batch correcto puede ser suficiente. Si después hay muchas consultas o procesos concurrentes, se puede crear una tabla de estado actual mantenida por el importador.

## Futuras tablas para actualización de stock

Cuando pasemos de lectura a actualización de stock, probablemente harán falta:

1. `EbayStockUpdateBatch`
2. `EbayStockUpdateLine`
3. `EbayStockUpdateResult`

Estas tablas deberían guardar:

1. Stock leído desde EfA.
2. Stock calculado como vendible en eBay.
3. Stock enviado realmente a eBay.
4. Resultado de la llamada a eBay.
5. Mensajes de error y reintentos.

## Decisiones tomadas

1. El ZIP original descargado desde eBay se guardará solamente en la base de datos.
2. El histórico de imports se conservará durante 60 días.
3. `EbayItemId` se almacenará como `nvarchar(32)`.
4. El enlace inicial con EfA se basará en el SKU eBay, descomponiéndolo en `item` y `qtun`.
5. No se creará una tabla de mapeo inicial. Si no se encuentra la combinación `item + qtun` en EfA, se registrará una incidencia.
6. `qtun` se valida contra EfA; eBay no define las unidades de venta válidas para este proyecto.
7. Los SKUs repetidos se consideran incidencias en principio, pendientes de comprobación.
8. La limpieza automática diaria de históricos de más de 60 días debe estar siempre activa.
9. Las fechas de esta base de datos se guardarán como hora local Europe/Berlin.
10. La importación desde C# usará table-valued parameters y stored procedures idempotentes con `MERGE`.

## Decisiones pendientes

1. Confirmar si los SKUs repetidos detectados son errores reales de publicación o excepciones permitidas.

## Scripts DDL

Los scripts iniciales están en `Database/DDL`.

Orden de ejecución:

1. `001_CreateSchema.sql`
2. `002_CreateTypes.sql`
3. `003_CreateInventoryTables.sql`
4. `004_CreateInventoryImportTableTypes.sql`
5. `005_CreateInventoryImportProcedures.sql`
6. `006_CreateInventoryViews.sql`
7. `007_CreateMaintenanceProcedures.sql`
8. `008_CreateSqlAgentJobs.sql`
