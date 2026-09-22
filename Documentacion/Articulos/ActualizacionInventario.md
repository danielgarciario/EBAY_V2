# Actualización del inventario de eBay

## Objetivo

Implementar el mantenimiento manual del inventario publicado en eBay. El proceso compara el último stock reportado por eBay con el stock actualmente disponible para la venta y actualiza únicamente las líneas que presentan diferencias.

La primera versión se ejecuta a petición del usuario, registra toda la actividad en Serilog y no modifica la base de datos.

## API de eBay

La actualización se realizará con la llamada `ReviseInventoryStatus` de la Trading API:

[eBay Developers: ReviseInventoryStatus](https://developer.ebay.com/devzone/xml/docs/Reference/eBay/ReviseInventoryStatus.html)

Condiciones relevantes:

1. Solo se actualiza la cantidad; no se modifica el precio.
2. Cada llamada acepta entre una y cuatro líneas `InventoryStatus`.
3. Para identificar de forma inequívoca listings simples y variaciones se enviarán siempre `ItemID` y `SKU`.
4. El valor enviado en `Quantity` será el stock disponible para la venta, columna `enventa`.
5. Una cantidad igual a cero es válida y se enviará normalmente.
6. Los listings se consideran creados mediante la interfaz clásica de eBay y compatibles con la Trading API.
7. El proceso no realizará reintentos automáticos. Un timeout después del envío se considerará un resultado incierto, porque no es posible saber si eBay llegó a aplicar la petición.
8. Los grupos se enviarán secuencialmente y el proceso continuará aunque uno de ellos falle.

## Integración con el software existente

Proyecto: `EBAY.InventoryService`

Namespace y carpeta: `EBAY.InventoryService.InventoryUpdate`

Componentes previstos:

1. `IInventoryUpdateService`: contrato público del proceso.
2. `InventoryUpdateService`: coordinación de lectura, validación, envío y resultados.
3. `InventoryUpdateRequests`: construcción y documentación de las peticiones XML.
4. Carpeta `Data`: modelos de entrada, respuesta y resultado.
5. Un origen de datos específico para leer las diferencias de inventario.

El servicio se registrará para Dependency Injection en `./EBAY.InventoryService/ServiceCollectionExtensions.cs`.

Solo se permitirá una ejecución simultánea dentro del mismo proceso. Si ya existe una actualización en curso, una segunda petición terminará inmediatamente con el estado correspondiente.

## Cliente HTTP

Todas las llamadas realizadas desde `EBAY.InventoryService` deben utilizar el proyecto `EBAYHttpClient`.

El proyecto expondrá clientes separados para:

1. OAuth: obtención y renovación del token de usuario.
2. APIs REST: autenticación mediante `Authorization: Bearer`.
3. Trading API XML: autenticación mediante `X-EBAY-API-IAF-TOKEN`.

Los tres clientes compartirán `EBAYClientOptions` y `OAuthTokenService`. No se duplicarán credenciales ni tokens.

El cliente Trading utilizará:

1. Endpoint predeterminado: `https://api.ebay.com/ws/api.dll`.
2. Versión predeterminada: `1477`.
3. Site ID predeterminado: `77`, correspondiente a Alemania.
4. Cabeceras `X-EBAY-API-CALL-NAME`, `X-EBAY-API-COMPATIBILITY-LEVEL`, `X-EBAY-API-SITEID` y `X-EBAY-API-IAF-TOKEN`.
5. Contenido `text/xml` codificado como UTF-8.

Las opciones nuevas mantendrán compatibilidad con las propiedades existentes para no exigir una migración inmediata de `appsettings.json`.

Configuración relevante:

```json
{
  "EBAYClient": {
    "TradingAPIBaseUrl": "https://api.ebay.com/ws/api.dll",
    "TradingAPIVersion": "1477",
    "EbayMarketPlaceID": "77"
  }
}
```

`TradingAPIBaseUrl` y `TradingAPIVersion` tienen esos valores predeterminados y pueden omitirse. `EbayMarketPlaceID` ya forma parte de la configuración existente. Las credenciales y el refresh token no se duplican en otra sección.

## Conexión con la base de datos

Todas las conexiones se crearán exclusivamente mediante `IEbayDatabaseConnectionFactory` del proyecto `EBAY.DatabaseConnection`.

No se crearán conexiones adicionales ni se modificará la base de datos durante este proceso.

Consulta de entrada:

```sql
SELECT [EbayItemId],
       [SellableSku],
       [ReportedQuantity],
       [enventa]
FROM [EBAY].[ebay].[BestandDifference];
```

Garantías de los datos de entrada:

1. `EbayItemId` y `SellableSku` identifican una única línea vendible.
2. No existen duplicados por `EbayItemId + SellableSku`.
3. `enventa` nunca es `NULL` ni negativo.
4. `ReportedQuantity` representa la cantidad observada en el último informe de eBay.
5. `enventa` representa la nueva cantidad que se debe enviar a eBay.

El código validará defensivamente estas condiciones. Las líneas inválidas no se enviarán y aparecerán en el resultado y en Serilog.

## Workflow

1. El usuario solicita una actualización.
2. Se intenta adquirir el bloqueo de ejecución local.
3. Se consultan las diferencias de inventario.
4. Si la consulta no devuelve filas, el proceso termina correctamente sin llamar a eBay.
5. Cada diferencia se registra en Serilog mediante propiedades separadas: `EbayItemId`, `SellableSku`, `ReportedQuantity` y `TargetQuantity`.
6. Se validan las filas y se detectan defensivamente claves duplicadas.
7. Las filas válidas se dividen en grupos de cuatro.
8. Para cada grupo se genera un `MessageID` único.
9. Se construye y envía una petición XML `ReviseInventoryStatus`.
10. Se deserializa la respuesta XML.
11. Se registra la respuesta y sus errores o advertencias en Serilog.
12. Si un grupo falla, se registra el fallo y se continúa con el siguiente.
13. Se devuelve un resumen agregado. No se modifica la base de datos.
14. Se libera el bloqueo local incluso si hay una excepción o cancelación.

Una nueva ejecución antes de importar otro Inventory Report puede enviar de nuevo las mismas diferencias. Este comportamiento es intencionado y se considera más seguro que asumir que el estado local ya coincide con eBay.

## Interpretación de la respuesta

1. `Success`: grupo actualizado correctamente.
2. `Warning`: grupo actualizado, pero se conservan y muestran las advertencias.
3. `PartialFailure`: parte del grupo pudo actualizarse; el resultado global será parcial.
4. `Failure`: grupo no actualizado.
5. Respuesta HTTP no satisfactoria: grupo fallido.
6. XML vacío, inválido o no reconocible: grupo fallido.
7. Timeout o interrupción de transporte después del envío: resultado incierto y sin reintento automático.

La cantidad ya no está garantizada en la respuesta moderna de eBay. Por tanto, la confirmación definitiva del stock llegará con una importación posterior del Inventory Report.

## Resultado público

El servicio no devolverá solamente un `bool`. El resultado incluirá como mínimo:

1. Estado global.
2. Número de diferencias leídas.
3. Número de líneas válidas e inválidas.
4. Número de líneas enviadas.
5. Número de líneas correctas, con advertencias, fallidas e inciertas.
6. Resultado de cada grupo, incluyendo `MessageID`, `CorrelationID`, `Ack` y errores de eBay.

## Logging

1. Se utilizará logging estructurado mediante `ILogger` y Serilog.
2. Cada diferencia tendrá propiedades independientes para permitir búsquedas y filtros.
3. Cada petición se correlacionará con su respuesta mediante `MessageID` y `CorrelationID`.
4. Se registrarán el estado HTTP, `Ack`, errores y advertencias.
5. Nunca se registrarán tokens OAuth, refresh tokens, App ID, Cert ID ni Dev ID.
6. Las respuestas de eBay se conservarán en el log, pero no en la base de datos.

## Concurrencia

La concurrencia simultánea es improbable, pero la aplicación impedirá dos ejecuciones dentro del mismo proceso mediante un bloqueo compartido.

Este bloqueo no coordina varias instancias de la aplicación. Una coordinación distribuida requeriría persistencia adicional y queda fuera del alcance de esta primera versión.

## Pruebas

Las pruebas automatizadas no se conectarán a SQL Server ni realizarán llamadas reales a eBay.

Casos mínimos:

1. Consulta sin diferencias.
2. División de líneas en grupos de cuatro.
3. Construcción del XML y escape de caracteres especiales del SKU.
4. Cabeceras y autenticación propias de la Trading API.
5. Deserialización de `Success`, `Warning`, `PartialFailure` y `Failure`.
6. Respuestas HTTP no satisfactorias.
7. XML vacío o inválido.
8. Validación de cantidades y duplicados.
9. Continuación después del fallo de un grupo.
10. Resultado incierto ante errores de transporte.
11. Cancelación.
12. Rechazo de una segunda ejecución simultánea.

La validación contra SQL Server y eBay se realizará posteriormente de forma manual por el usuario.

## Decisiones tomadas

1. `enventa` es la cantidad que se envía a eBay.
2. `ReportedQuantity` se usa para mostrar y registrar la diferencia.
3. `enventa` nunca es nulo ni negativo.
4. La cantidad cero se envía normalmente.
5. Se continúa con los demás grupos después de un fallo.
6. No hay reintentos automáticos.
7. No se modifica la base de datos.
8. Se permiten reenvíos en ejecuciones posteriores hasta que un nuevo Inventory Report refleje el cambio.
9. Se impide concurrencia dentro del mismo proceso.
10. El contrato público devuelve un resultado detallado.
11. Los logs de cada línea serán estructurados.
12. Las pruebas automáticas utilizarán dobles de prueba para HTTP y base de datos.
