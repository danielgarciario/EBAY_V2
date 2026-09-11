# Grupo de trabajo: Artículos

## Objetivo

Gestionar la información de artículos entre eBay y EfA.

El flujo de artículos debe:

1. Leer los artículos activos publicados en eBay.
2. Guardar esa información en una base de datos propia.
3. Comprobar que los artículos de eBay son compatibles con los artículos del ERP `EfA`.
4. Obtener de EfA el stock disponible real.
5. Enviar a eBay una actualización del stock disponible.

## Alcance inicial

La investigación actual se centra en la obtención de artículos activos desde eBay.

Hay dos flujos candidatos:

- Trading API con `GetMyeBaySelling` y `ActiveList`.
- Sell Feed API con `createInventoryTask`, `getInventoryTask` y `getResultFile`.

La hipótesis de trabajo actual es usar Sell Feed API como vía principal para obtener los artículos activos.

## Flujo 1: Trading API con GetMyeBaySelling

`GetMyeBaySelling` permite leer información de la sección "All Selling" de la cuenta eBay autenticada. Para obtener los artículos activos se usaría el contenedor `ActiveList`.

Flujo básico:

1. Llamar a `GetMyeBaySelling`.
2. Solicitar `ActiveList` con `Include = true`.
3. Recorrer la paginación hasta recuperar todos los artículos activos.
4. Guardar los artículos en la base de datos.
5. Calcular la cantidad disponible cuando sea necesario.

Punto importante sobre cantidades:

- En objetos tipo `Item`, `Quantity` representa la cantidad total: unidades disponibles + unidades vendidas.
- La cantidad disponible se calcula como `Quantity - SellingStatus.QuantitySold`.
- Para variaciones, aplica la misma idea: `Variation.Quantity - Variation.SellingStatus.QuantitySold`.

Ventajas:

- Encaja con el cliente Trading API que ya existe en la solución.
- Puede devolver bastante detalle del listing.
- Es útil para consultar o contrastar casos individuales.

Inconvenientes:

- Es un flujo paginado y síncrono.
- Tiene límites de llamada y conviene evitarlo como mecanismo masivo frecuente.
- Hay que tener cuidado con el cálculo de cantidad disponible.
- Puede ser menos cómodo para procesos batch de inventario completo.

Referencias:

- eBay Trading API `GetMyeBaySelling`: https://developer.ebay.com/devzone/xml/docs/Reference/eBay/GetMyeBaySelling.html

## Flujo 2: Sell Feed API con Active Inventory Report

Sell Feed API permite generar informes descargables mediante tareas asíncronas. Para artículos activos, el flujo usa `LMS_ACTIVE_INVENTORY_REPORT`.

Flujo básico:

1. Crear una tarea con `createInventoryTask`.
2. Indicar `feedType = LMS_ACTIVE_INVENTORY_REPORT`.
3. Indicar `schemaVersion = 1.0`.
4. Opcionalmente filtrar por tipo de listing con `filterCriteria.listingFormat`, por ejemplo `AUCTION` o `FIXED_PRICE`.
5. Leer el `taskId` desde la cabecera `Location` de la respuesta.
6. Consultar el estado con `getInventoryTask`.
7. Esperar a que el estado sea `COMPLETED`.
8. Descargar el resultado con `getResultFile`.
9. Procesar el fichero descargado. Según la documentación de eBay, el resultado se descarga como fichero comprimido ZIP.
10. Guardar los datos en la base de datos.

Según la documentación de eBay, el informe de inventario activo contiene información de precio y cantidad de los listings activos y de sus variaciones.

Ventajas:

- Mejor encaje para lectura masiva de inventario activo.
- Flujo batch asíncrono preparado para grandes volúmenes.
- Devuelve un fichero descargable que se puede guardar como evidencia de importación.
- Evita depender de paginación síncrona para el inventario completo.

Inconvenientes:

- El flujo es más largo porque requiere crear tarea, consultar estado y descargar fichero.
- Hay que implementar almacenamiento del task id, estado, fichero descargado y errores.
- Hay que inspeccionar el formato exacto del fichero resultante y mapear sus campos.
- Es posible que no contenga todo el detalle del listing; puede que se necesite Trading API como apoyo para casos concretos.

Referencias:

- eBay Sell Feed API, flujo de informes descargables: https://developer.ebay.com/api-docs/sell/static/feed/merchant-data-downloadable-reports-flow.html
- eBay Sell Feed API `getResultFile`: https://developer.ebay.com/api-docs/sell/feed/resources/task/methods/getResultFile
- eBay Listing Management, obtención de active listings con Sell Feed API: https://developer.ebay.com/develop/guides/sell/listing-management

## Decisión provisional

Usar Sell Feed API como flujo principal para obtener los artículos activos de eBay.

Trading API queda como flujo secundario para:

- Contrastar datos.
- Consultar detalles que no vengan en el informe.
- Resolver incidencias concretas de listings individuales.
- Mantener compatibilidad con código ya existente en la solución.

## Persistencia inicial esperada

La base de datos propia debería guardar los datos del informe como snapshots de importación y como unidades vendibles normalizadas.

El XML real analizado se documenta en [Active Inventory Report real de eBay](Articulos/InventoryReportXml.md).

La propuesta inicial de tablas se documenta en [Modelo de datos para artículos](Articulos/ModeloDatosArticulos.md).

La propuesta de importación idempotente desde C# se documenta en [Importación idempotente de artículos](Articulos/ImportacionIdempotente.md).

El fichero de Sell Feed API observado trae, como mínimo:

- Identificador del listing en eBay.
- SKU o identificador usado para enlazar con EfA.
- Precio publicado.
- Moneda.
- Cantidad informada por eBay.
- Variaciones y sus características cuando existen.
- Fecha y hora de importación.
- Task id de Sell Feed API cuando aplique.
- Fichero original descargado o referencia al fichero original.

El fichero real no trae título, estado textual, categoría, descripción ni fotos del listing. Si esos datos son necesarios, habrá que enriquecer el inventario con otro flujo.

## Compatibilidad con EfA

Para considerar compatible un artículo de eBay con EfA habrá que definir:

- Qué campo enlaza eBay con EfA: SKU, número de artículo, referencia interna u otro.
- Si un listing eBay puede corresponder a un solo artículo EfA o a varios.
- Cómo tratar variaciones.
- Cómo tratar packs, bundles o artículos virtuales.
- Qué hacer con listings activos que no existan en EfA.
- Qué hacer con artículos EfA sin listing activo en eBay.

## Actualización de stock hacia eBay

El stock disponible que se enviará a eBay debe venir de EfA.

Queda pendiente definir:

- Qué stock de EfA se considera vendible en eBay.
- Si se aplican reservas, stock mínimo o margen de seguridad.
- Si el stock eBay puede ser menor que el stock EfA por estrategia comercial.
- Frecuencia de actualización.
- API final para actualizar stock.
- Política de reintentos y auditoría.

## Preguntas abiertas

- ¿Qué marketplace eBay se usará inicialmente?
- ¿Los listings usan SKU coincidente con EfA?
- ¿Hay listings con variaciones?
- ¿Hay bundles o productos compuestos?
- ¿Qué volumen de listings activos hay actualmente?
- ¿Cada cuánto hay que refrescar el inventario completo?
- ¿Cada cuánto hay que enviar stock actualizado a eBay?
- ¿Se guardará el ZIP original descargado como evidencia histórica?
- ¿Dónde se guardarán los ficheros descargados: base de datos, filesystem o ambos?
