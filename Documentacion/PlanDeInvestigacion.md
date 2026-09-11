# Plan de investigación

## Objetivo

Definir qué debe hacer `EBAY_V2`, qué sistemas debe conectar y qué arquitectura conviene antes de escribir código.

El proyecto se divide inicialmente en tres grupos de trabajo: artículos, pedidos y envíos. La investigación actual se centra en el grupo de artículos.

## Preguntas principales

1. ¿Cuál es el objetivo principal del proyecto?
   - Importar pedidos de eBay.
   - Publicar o actualizar productos en eBay.
   - Sincronizar stock y precios.
   - Gestionar mensajes, incidencias o estados de envío.
   - Crear una base común para eBay y Amazon.

2. ¿Cuál será el sistema maestro para productos, precios y stock?
   - EfA / Infor LN 6.7.
   - Shopware / handwerker3000.de.
   - Una base propia en SQL Server.
   - Un flujo mixto con reglas por canal.

3. ¿Qué datos deben sincronizarse con eBay?
   - Productos.
   - Variantes.
   - Imágenes.
   - Categorías.
   - Precios.
   - Stock.
   - Pedidos.
   - Envíos.
   - Estados de pago.

4. ¿Qué APIs de eBay hay que usar?
   - Trading API.
   - Sell Feed API.
   - Browse API.
   - Inventory API.
   - Fulfillment API.
   - Taxonomy API.
   - OAuth y gestión de tokens.

5. ¿Qué restricciones operativas hay?
   - Límites de API.
   - Frecuencia de sincronización.
   - Tolerancia a errores.
   - Reintentos.
   - Auditoría.
   - Alertas.

## Áreas de análisis

### eBay

- Revisar qué APIs siguen siendo recomendadas para cada caso de uso.
- Confirmar qué parte se resuelve mejor con Trading API y qué parte con Sell Feed API.
- Documentar autenticación, scopes, renovación de tokens y entornos sandbox/producción.
- Identificar límites, cuotas y requisitos de certificación si existen.

### Artículos

- Documentar las dos alternativas para obtener artículos activos de eBay:
  - Trading API con `GetMyeBaySelling` y `ActiveList`.
  - Sell Feed API con `createInventoryTask`, `getInventoryTask` y `getResultFile`.
- Evaluar Sell Feed API como primera opción para la lectura masiva de artículos activos.
- Definir qué datos del informe de eBay deben guardarse en SQL Server.
- Definir cómo se compara cada artículo de eBay con su artículo correspondiente en EfA.
- Definir cómo se calcula el stock que se enviará de EfA a eBay.

### Datos maestros

- Definir de dónde salen productos, precios, stock y descripciones.
- Identificar claves comunes entre EfA, Shopware, eBay y Amazon.
- Decidir cómo manejar productos que solo existen en algunos canales.

### Arquitectura

- Decidir si el proyecto será una librería, servicio Windows, aplicación web, job programado o combinación.
- Definir si necesita base de datos propia en SQL Server.
- Diseñar logging, configuración, secretos y despliegue.

### Operaciones

- Definir cómo se monitorizan errores.
- Definir cómo se reintentan sincronizaciones fallidas.
- Definir qué necesita ver o corregir un usuario interno.

## Entregables de la fase de planificación

- Alcance funcional confirmado.
- Lista de sistemas y APIs implicadas.
- Modelo inicial de datos.
- Arquitectura propuesta.
- Decisiones pendientes y riesgos.
- Plan de implementación por fases.

## Decisiones abiertas

- Confirmar si `EBAY_V2` será solo para eBay o si se diseñará desde el principio pensando en Amazon. De momento el alcance principal es eBay + EfA.
- Confirmar si EfA será el sistema maestro. Para stock de artículos se asume inicialmente que EfA será el origen principal.
- Confirmar si la integración será automática, manual asistida o mixta.
- Confirmar dónde se desplegará el primer componente real.
- Confirmar cómo se gestionarán credenciales y secretos.
- Confirmar si Sell Feed API será la vía principal para leer artículos activos.
