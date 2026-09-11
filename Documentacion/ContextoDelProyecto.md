# Contexto del proyecto EBAY_V2

## Estado actual

`EBAY_V2` está en fase de investigación y planificación. La prioridad actual es documentar el alcance y entender qué se quiere construir antes de implementar cambios de código.

El objetivo del proyecto es gestionar la comunicación entre eBay y EfA.

La solución contiene actualmente tres proyectos:

- `EBAY.TRADING.API.CLIENT`: cliente relacionado con eBay Trading API.
- `EBAYHttpClient`: librería o experimento para cliente HTTP de eBay.
- `TestConsole`: aplicación de consola para pruebas e integración inicial.

Los proyectos están configurados para `.NET 10`.

## Contexto empresarial

La empresa trabaja con varios canales y sistemas:

- ERP principal: Infor LN 6.7, conocido internamente como `EfA`.
- Tienda online principal: `handwerker3000.de`, también llamada `Shopware`, `handwerker`, `handwerker3000` o `hw3k`.
- Marketplaces adicionales: eBay y Amazon.

Las tiendas de eBay y Amazon venden solo una parte del catálogo disponible, no necesariamente todos los productos de EfA o Shopware.

## Infraestructura conocida

- Dominio Windows local: `STOCK.local`.
- Centro de datos externo: `Continum`.
- Equipo principal de desarrollo: `ST-LILLIPUT05.stock.local`.
- Logs centralizados con Serilog hacia `demoubu.stock.local`.
- Despliegues habituales en Microsoft Windows Server con IIS.
- Active Directory con 3 domain controllers en Windows Server 2012.

## Sistemas potencialmente relacionados

El proyecto puede necesitar relacionarse con:

- eBay Trading API.
- eBay Sell Feed API.
- Amazon Marketplace, si el alcance crece hacia sincronización multicanal.
- EfA / Infor LN 6.7 como sistema maestro empresarial.
- Shopware / handwerker3000.de como canal online basado en MySQL.
- Microsoft 365 y Teams para flujos internos, avisos o soporte operativo.

## Grupos de trabajo

### Artículos

El grupo de artículos se encarga de leer los artículos activos en eBay, guardarlos en una base de datos, comprobar que son compatibles con los artículos del ERP `EfA` y enviar a eBay actualizaciones del stock disponible según los datos de EfA.

### Pedidos

El grupo de pedidos se encarga de vigilar los pedidos de eBay, detectar nuevos pedidos, importarlos en la base de datos y procesarlos para importarlos en EfA. Este proceso puede incluir creación de clientes, pedidos, posiciones, direcciones y otros datos necesarios.

### Envíos

El grupo de envíos se encarga de reportar a eBay el número de seguimiento cuando el paquete ya se ha generado y se ha enviado electrónicamente a la empresa de paquetería.

## Principios iniciales

- Preferir C# y .NET 10.
- Preferir SQL Server para almacenamiento propio del proyecto, salvo que el sistema existente indique otra cosa.
- Usar Serilog para logging.
- Pensar en despliegue Windows Server/IIS salvo que el componente sea claramente Linux o marketplace específico.
- Diseñar con separación clara entre integración externa, lógica de negocio y persistencia.
