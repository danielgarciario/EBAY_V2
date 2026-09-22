# Objetivo.

El objetivo de este documento es determinar Como se va a realizar el mantenimiento del inventario dentro del sistema de EBAY.

# Definiciones de EBAY:

La documentacion de Ebay es:
[eBay Dev](https://developer.ebay.com/devzone/xml/docs/Reference/eBay/ReviseInventoryStatus.html)

Hay que programar el ReviseInventoryStatus funcion dentro de nuestro codigo.

# Integracion con el software existente.

Quiero que lo integres en:
Proyecto: EBAY.InventoryService
Servicio: EBAY.InventoryUpdate

Genera un fichero con el Servicio y una carpeta con los records o clases que necesites como Request / Response en una carpeta dentro de la carpeta del servicio. En otros proyectos lo llamo Data, pero puedes escoger el que te plazca y que tenga sentido.

Normalmente suelo crear una clase statica con los request para poder documentarlo.
Un ejemplo lo tienes `.\EBAY.InventoryService\FeedAPI\FeedAPIRequests.cs` lo suelo usar para recopilar en un unico sitio todos los reques relevantes y documentarlo.

Queiero que lo integres dentro de
`.\EBAY.InventoryService\ServiceCollectionExtensions.cs` aqui esta la definicion para Dependency Injection


## HTTP Client.

Utiliza por favor el cliente de `.\EBAYHttpClient`.
La gestion de los tokens funciona correctamente en este proyecto.
Por favor asegurate que todos las llamadas de `.\EBAY.InventoryService`:
1. Se puedan hacer desde el proyecto de `\EBAYHttpClient`
2. Todas usan ese proyecto, sino hay que cambiarlo.


## Conexion con la base de datos:

Utiliza por favor el connexion factory de `.\EBAY.DatabaseConnection`

Como en el caso anterior, que todas las conexiones a la base de datos se hagan con y desde Connexion Factory.

# Workflow.

El flujo previsto es el siguiente:
1. A peticion del usuario se realiza un llamada a la base de datos:

```sql
SELECT [EbayItemId]
      ,[SellableSku]
      ,[ReportedQuantity]
      ,[enventa]
  FROM [EBAY].[ebay].[BestandDifference]
```

Esto ya está programado en SQL Server y devuelve solamente aquellos articulos que tienen una diferencia entre el Stock Reportado y lo que Esta Disponible actualmente par la venta.

2. Si no hay articulos con diferencias entre ReportedQuantity y en Venta, se acaba
3. Hacer un log de la tabla (en Serilog)
4. Llamar al procedimiento ReviseInventoryStatus.
5. Deserilizar la respuesta. 
6. Logar (en serilog) la respuesta. No se modifica la base de datos.
