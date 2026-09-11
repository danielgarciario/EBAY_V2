# Active Inventory Report real de eBay

## Fichero analizado

Fichero real añadido para investigación:

`Documentacion/Articulos/activeinventory-29186083323906-Sep-11-2026-01-57-17-0700.xml`

Es un resultado de `LMS_ACTIVE_INVENTORY_REPORT` obtenido mediante Sell Feed API.

## Estructura observada

El XML usa el namespace por defecto:

`urn:ebay:apis:eBLBaseComponents`

Raíz:

- `BulkDataExchangeResponses`
  - `ActiveInventoryReport`
    - muchos `SKUDetails`
    - `Ack`

Cada `SKUDetails` representa un listing de eBay y contiene:

- `SKU`
- `Quantity`
- `ItemID`
- `Price`, solo en listings sin variaciones
- `Variations`, solo en listings con variaciones

Cada `Variation` contiene:

- `SKU`
- `Price`
- `Quantity`
- `VariationSpecifics`

Cada `VariationSpecifics` contiene uno o varios `NameValueList` con:

- `Name`
- `Value`

## Estadísticas del fichero

Datos observados en este fichero:

- `Ack`: `Success`
- `SKUDetails`: 600
- Listings sin variaciones: 373
- Listings con variaciones: 227
- Variaciones: 879
- Filas `NameValueList`: 1025
- Monedas observadas: `EUR`
- `ItemID` únicos: 600
- Filas vendibles normalizadas: 1252
- Claves únicas `ItemID + SKU vendible`: 1252

En los 227 listings con variaciones, `SKUDetails.Quantity` coincide con la suma de las cantidades de sus variaciones.

## Duplicados relevantes

El `ItemID` es único en el fichero, pero algunos SKUs aparecen en más de un listing:

- `101156.pa`
- `32868.pa`
- `33727.St`
- `48218.St`

También hay SKUs padre repetidos:

- `32868.pa`
- `PAR3405`
- `PAR3491`

Conclusión: para almacenar datos propios de eBay no conviene usar solo `SKU` como clave única. La clave natural mínima observada para una fila vendible es `ItemID + SKU vendible`.

El SKU sigue siendo importante como candidato de enlace con EfA, pero no debe asumirse globalmente único dentro de eBay.

## Observaciones sobre listings con variaciones

En un listing con variaciones:

- El `SKUDetails.SKU` actúa como SKU padre.
- `SKUDetails.Quantity` actúa como cantidad total reportada del listing padre.
- El precio no aparece en el padre.
- Cada `Variation` tiene su propio SKU, precio y cantidad.
- Cada `Variation` puede tener una o varias características, por ejemplo `Breite=30 mm` y `VPE=Rolle`.

Para el modelo de datos, una variante debe tratarse como unidad vendible propia.

## Observaciones sobre listings simples

En un listing sin variaciones:

- El SKU vendible es el mismo que `SKUDetails.SKU`.
- El precio está en `SKUDetails.Price`.
- La cantidad está en `SKUDetails.Quantity`.
- No hay `VariationSpecifics`.

## Opinión sobre las clases XmlSerializer propuestas

La idea de usar `XmlSerializer` es válida para este fichero, pero las clases propuestas necesitan algunos ajustes antes de usarlas como DTOs estables.

Cambios recomendados:

- Declarar el namespace de eBay explícitamente: `urn:ebay:apis:eBLBaseComponents`.
- No modelar `ItemID` como `double`. Es un identificador externo, no una cantidad. En C# debería ser `string` o `long`; preferencia inicial: `string` en la frontera de integración.
- No modelar importes como `double`. Para precios usar `decimal`.
- Renombrar el valor textual de `Price` a algo semántico como `Amount`.
- Hacer `Price` opcional en `SKUDetails`, porque los listings con variaciones no lo tienen.
- Hacer `Variations` opcional en `SKUDetails`, porque los listings simples no lo tienen.
- Inicializar listas vacías cuando sea práctico para evitar comprobaciones nulas innecesarias después de deserializar.
- No hace falta guardar una propiedad `Text` en `BulkDataExchangeResponses`; el texto observado es solo whitespace entre nodos.

La clase DTO no debería ser la entidad de base de datos. Lo ideal es:

1. Deserializar el XML a DTOs.
2. Normalizar esos DTOs a un modelo interno.
3. Guardar el modelo interno en tablas diseñadas para importación, auditoría y comparación con EfA.

## Campos que el feed no trae en la muestra

El fichero real no muestra:

- Título del listing.
- Estado textual del listing.
- Categoría.
- Descripción.
- Fotos.
- Datos de envío.
- Estado de compatibilidad con EfA.
- Cantidad vendida histórica.

Si esos datos hacen falta, habrá que enriquecer el inventario con otro flujo, probablemente Trading API u otra API de eBay.
