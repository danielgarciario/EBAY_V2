# EBAY_V2 - Documentación

Esta carpeta contiene la documentación de investigación y planificación del proyecto `EBAY_V2`.

El objetivo de esta fase es entender el alcance, los sistemas implicados, las restricciones técnicas y las decisiones pendientes antes de escribir código.

## Documentos

- [Contexto del proyecto](ContextoDelProyecto.md): información conocida, vocabulario interno, sistemas conectados y restricciones.
- [Plan de investigación](PlanDeInvestigacion.md): preguntas abiertas, áreas que hay que estudiar y entregables esperados.
- [Artículos](Articulos.md): investigación del grupo de trabajo de artículos, lectura de inventario activo en eBay y actualización de stock desde EfA.
- [Active Inventory Report real de eBay](Articulos/InventoryReportXml.md): análisis del XML real descargado desde eBay.
- [Modelo de datos para artículos](Articulos/ModeloDatosArticulos.md): propuesta inicial de tablas para guardar inventario, variaciones y compatibilidad con EfA.
- [Importación idempotente de artículos](Articulos/ImportacionIdempotente.md): TVPs y stored procedures para importar el inventory report desde C#.

## Reglas de trabajo para esta fase

- No escribir código hasta que se cierre una dirección técnica clara.
- Documentar primero las decisiones, dudas y dependencias.
- Separar hechos confirmados de hipótesis.
- Mantener los textos internos en castellano.
- Mantener nombres técnicos, identificadores y futuros comentarios de código en inglés.
- Preparar los textos visibles para usuarios finales en alemán cuando llegue la fase de implementación.
