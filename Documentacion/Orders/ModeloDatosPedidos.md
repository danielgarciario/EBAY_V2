# Modelo de datos para pedidos de eBay

## Objetivo

Conservar los pedidos recibidos desde la Fulfillment API de eBay para que, en fases posteriores, puedan crearse directamente en EfA y sirvan de referencia para notificar expediciones a eBay.

El alcance actual es una sola cuenta y un único mercado:

1. Vendedor: `handwerker3000_de`.
2. Marketplace de venta y compra: `EBAY_DE`.
3. Instrucción de fulfillment admitida: `SHIP_TO`.
4. No se crean todavía tablas de paquetes, tracking ni comunicación de expediciones.

## Decisiones tomadas

1. `orderId` es la clave externa única del pedido y la referencia de idempotencia hacia EfA.
2. Solo se consideran aptos para EfA pedidos con estado de pago `PAID`.
3. No existe una cola de revisión manual. El estado `EfaImportStatus` es técnico y prepara la futura comunicación directa con EfA.
4. EfA recibirá siempre un cliente nuevo; este modelo no intenta deduplicar clientes.
5. Cada línea eBay se conserva como una posición con su cantidad. Su SKU sigue el patrón `{item}.{qtun}` de EfA.
6. Se guardan descuentos, portes, comisiones de eBay, promociones, impuestos, cancelaciones y reembolsos. No se guardan los pagos individuales.
7. Cada importación almacena el JSON original de eBay durante 30 días. Los datos normalizados se conservan seis meses desde la última importación del pedido.
8. Se admiten dos orígenes de entrada: `Polling` y `Webhook`. Ambos usan el mismo procedimiento y modelo.
9. Todas las fechas de la base de datos se guardan como hora local Europe/Berlin, sin nombres ni valores por defecto UTC.
10. La importación usa TVPs y `MERGE` para ser idempotente. Una respuesta retrasada no puede reemplazar un pedido cuya `lastModifiedDate` sea más reciente.

El cliente C# debe convertir cada timestamp ISO 8601 recibido de eBay a Europe/Berlin antes de llenar los TVPs. El JSON original preserva el valor exacto recibido durante su período de 30 días.

## Fuentes eBay

1. [getOrders](https://developer.ebay.com/api-docs/sell/fulfillment/resources/order/methods/getOrders) permite recuperar pedidos de la cuenta autenticada y filtrar por creación, modificación o estado de fulfillment.
2. [Guía de descubrimiento de pedidos](https://developer.ebay.com/api-docs/sell/static/orders/discovering-unfulfilled-orders.html) documenta la búsqueda por creación, modificación y estado. Las condiciones históricas de eBay han cambiado con el tiempo; la persistencia propia y el sondeo continuo siguen siendo necesarios para no depender de ese límite externo.
3. [Guía de fulfillment](https://www.developer.ebay.com/api-docs/sell/static/orders/managing-fulfillments.html) confirma que un pedido puede tener varios fulfillment. Esa parte se modelará cuando se implemente el envío de números de seguimiento.

## Modelo relacional

```text
OrderImportBatch
        │  última importación
        └───────────────< EbayOrder >───────────────1 EbayOrderShippingAddress
                               │
                               ├───────────────< EbayOrderLineItem
                               │                       ├──< EbayOrderLineItemVariationAspect
                               │                       ├──< EbayOrderLineItemPromotion
                               │                       └──< EbayOrderLineItemTax
                               │
                               └───────────────< EbayOrderRefund
```

### `OrderImportBatch`

Representa un payload recibido, ya sea por sondeo o webhook. Guarda el JSON original, su hash SHA-256, referencia del origen y fechas técnicas.

1. `SourceContentRetentionUntilLocal` expira 30 días tras la importación. La limpieza convierte `SourceContent` en `NULL`, manteniendo su hash y metadatos.
2. `RetentionUntilLocal` expira seis meses tras la importación.
3. Un batch no se elimina mientras sea la última importación de un pedido conservado.

### `EbayOrder`

Es la representación actual normalizada de un pedido eBay. Tiene una restricción única sobre `EbayOrderId`.

Incluye:

1. Identificadores eBay: `EbayOrderId`, `LegacyOrderId` y `SalesRecordReference`.
2. Estados de pedido, pago y cancelación.
3. Totales comerciales y comisiones de marketplace.
4. Datos fiscales y de registro del comprador, necesarios para disponer de la información de origen aunque el destino real sea la dirección `SHIP_TO`.
5. Estado técnico de importación a EfA: `Pending`, `Exported` o `Failed`, junto con la futura referencia generada por EfA.
6. Trazabilidad de la primera y última importación, y fecha de retención.

### `EbayOrderShippingAddress`

Contiene una única dirección `SHIP_TO` por pedido. Al operar solo en Alemania, el DDL exige `CountryCode = 'DE'`. También se guardan transportista y servicio solicitados por eBay, además de la ventana estimada de entrega.

### `EbayOrderLineItem`

Guarda cada línea vendida, identificada de forma única dentro del pedido por `EbayLineItemId`.

Incluye SKU, título, cantidad, importes de línea, descuentos, portes, fechas de preparación y entrega, marketplace y ubicación del artículo. El DDL exige `EBAY_DE` tanto en el marketplace de listing como en el de compra.

Las tablas hijas conservan los datos repetibles de la respuesta:

1. `EbayOrderLineItemVariationAspect`: aspectos de variación.
2. `EbayOrderLineItemPromotion`: promociones y descuentos aplicados.
3. `EbayOrderLineItemTax`: impuestos normales y los recaudados/remitidos por eBay, distinguidos por `TaxKind`.

### `EbayOrderRefund`

Guarda el estado, fecha e importe de cada reembolso. El importador debe proporcionar el identificador de reembolso devuelto por eBay como `EbayRefundId`.

## Importación idempotente

El procedimiento `[ebay].[ImportOrders]` recibe el payload JSON y siete TVPs:

1. `OrderImportTableType`
2. `OrderShippingAddressImportTableType`
3. `OrderLineItemImportTableType`
4. `OrderLineItemVariationAspectImportTableType`
5. `OrderLineItemPromotionImportTableType`
6. `OrderLineItemTaxImportTableType`
7. `OrderRefundImportTableType`

El proceso valida que la cuenta, marketplace, dirección y relaciones recibidas estén dentro del alcance definido. Después crea un batch y persiste el pedido y sus hijos en una única transacción.

Para cada `EbayOrderId`:

1. Si no existe, lo inserta.
2. Si existe y el `LastModifiedDateLocal` recibido es igual o posterior al almacenado, actualiza cabecera y colecciones hijas.
3. Si el dato recibido es más antiguo, lo conserva en el JSON de su batch, pero no reemplaza el estado normalizado más reciente.

## Implementación C#

El proyecto `EBAY.OrdersService` implementa la primera importación por sondeo:

1. `IOrderImportService.ImportRecentOrdersAsync()` consulta `getOrders` desde una ventana solapada configurable. El valor inicial es 15 minutos.
2. `IOrderImportService.ImportModifiedOrdersAsync(DateTime fromLocal)` permite ejecutar una carga desde una fecha local Europe/Berlin concreta, útil para pruebas y futura carga histórica.
3. Se usa el cliente existente `IEBayHttpClientFactory` y la fábrica existente `IEbayDatabaseConnectionFactory`; no se incorpora Dapper porque los TVPs de la importación se representan de forma directa y tipada con `SqlCommand` y `DataTable`.
4. El servicio solicita `fieldGroups=TAX_BREAKDOWN`, recorre todas las páginas que devuelva eBay y persiste cada página como un `OrderImportBatch` independiente.
5. La configuración está en la sección `EBAYOrders`: `PollingLookbackMinutes` y `PageSize`. El tamaño de página admitido es de 1 a 200.
6. El cliente OAuth debe solicitar la scope `https://api.ebay.com/oauth/api_scope/sell.fulfillment`. Si el refresh token actual no fue autorizado con ella, deberá obtenerse uno nuevo antes de probar la importación.

`TestConsole` registra el servicio mediante `services.AddEbayOrdersService(config)`. No se ha añadido una invocación automática para evitar cambiar el flujo actual de ejecución del inventario.

## Vistas

1. `[ebay].[vwLatestOrderImportBatch]`: última recepción y disponibilidad del JSON original.
2. `[ebay].[vwEbayOrderOverview]`: consulta operativa de pedido, importes, dirección y estado EfA.
3. `[ebay].[vwEbayOrdersReadyForEfa]`: pedidos `PAID`, no cancelados, no enviados, con dirección y un SKU sintácticamente compatible con `{item}.{qtun}`. No sustituye la validación posterior contra EfA.
4. `[ebay].[vwEbayOrderLineItemDetail]`: líneas con desglose comercial y SKU separado para facilitar consultas EfA.

## Retención y limpieza

`[ebay].[CleanupOrderHistory]` debe ejecutarse diariamente:

1. Borra el JSON de batches cuyo plazo de 30 días haya vencido.
2. Elimina pedidos y todos sus hijos por cascada cuando han vencido los seis meses.
3. Elimina batches que ya han vencido y que no son la última importación de ningún pedido retenido.

El script `016_CreateOrderCleanupSqlAgentJob.sql` registra el job diario a las 02:30. No debe ejecutarse si la instancia no dispone de SQL Server Agent.

## Scripts DDL

Ejecutar después de los scripts existentes de inventario:

1. `010_CreateOrderTypes.sql`
2. `011_CreateOrderTables.sql`
3. `012_CreateOrderImportTableTypes.sql`
4. `013_CreateOrderImportProcedure.sql`
5. `014_CreateOrderViews.sql`
6. `015_CreateOrderMaintenanceProcedure.sql`
7. `016_CreateOrderCleanupSqlAgentJob.sql`, únicamente si existe SQL Server Agent.

## Fuera del alcance actual

1. Carga histórica de pedidos existentes.
2. Llamadas a EfA, creación de clientes y creación de pedidos en EfA.
3. Validación de existencia del SKU frente a EfA.
4. Paquetes, envíos parciales, números de seguimiento y llamadas `shipping_fulfillment` a eBay.
5. Reintentos y auditoría de llamadas a EfA o a eBay.
