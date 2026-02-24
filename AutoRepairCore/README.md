# AutoRepairCore - Sistema de Gestión de Taller Automotriz

Sistema de gestión para talleres de reparación automotriz desarrollado con ASP.NET Core 8.0 y Razor Pages.

## Características

- ? CRUD completo de Órdenes de Servicio
- ? Selección de clientes y vehículos
- ? Gestión de estados de órdenes (Abierta, En proceso, Finalizada, Cancelada)
- ? Seguimiento de fechas de entrada y entrega
- ? Cálculo de costos
- ? Interfaz responsiva con Bootstrap 5

## Estructura del Proyecto

```
AutoRepairCore/
??? Models/                    # Modelos de entidades
?   ??? Customer.cs           # Clientes
?   ??? Vehicle.cs            # Vehículos
?   ??? ServiceOrder.cs       # Órdenes de servicio
?   ??? Mechanic.cs           # Mecánicos
?   ??? Service.cs            # Servicios
?   ??? Replacement.cs        # Refacciones
?   ??? OrderService.cs       # Relación orden-servicio
?   ??? OrderReplacement.cs   # Relación orden-refacción
?   ??? OrderMechanic.cs      # Relación orden-mecánico
??? Data/                     # Contexto de base de datos
?   ??? AutoRepairDbContext.cs
??? Pages/                    # Razor Pages
?   ??? ServiceOrders/       # CRUD de órdenes
?   ?   ??? Index.cshtml
?   ?   ??? Create.cshtml
?   ?   ??? Edit.cshtml
?   ?   ??? Details.cshtml
?   ?   ??? Delete.cshtml
?   ??? Shared/              # Layout compartido
??? wwwroot/                 # Archivos estáticos
```

## Requisitos Previos

- .NET 8.0 SDK
- SQL Server (LocalDB o instancia completa)
- Visual Studio 2022 o VS Code

## Configuración de Base de Datos

1. La base de datos debe existir previamente. Ejecuta el script SQL proporcionado para crear:
   - Base de datos `AutoRepairDB`
   - Todas las tablas necesarias
   - Datos de ejemplo

2. La cadena de conexión está configurada en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AutoRepairDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Instalación

1. Clona o descarga el proyecto

2. Restaura los paquetes NuGet:
```bash
dotnet restore
```

3. Verifica la cadena de conexión en `appsettings.json`

4. Ejecuta el proyecto:
```bash
dotnet run
```

5. Abre tu navegador en `https://localhost:7xxx` (el puerto se mostrará en la consola)

## Uso del Sistema

### Órdenes de Servicio

#### Crear Nueva Orden
1. Ve a "Órdenes de Servicio" en el menú de navegación
2. Haz clic en "Crear Nueva Orden"
3. Selecciona un cliente de la lista
4. El sistema cargará automáticamente los vehículos del cliente
5. Selecciona el vehículo
6. Completa los datos de la orden:
   - Fecha de entrada
   - Fecha estimada de entrega
   - Estado inicial
   - Costo
7. Haz clic en "Crear"

#### Ver Órdenes
- La página principal muestra todas las órdenes con:
  - Folio
  - Información del cliente
  - Vehículo
  - Fechas
  - Estado (con código de colores)
  - Costo

#### Editar Orden
1. Haz clic en "Editar" en cualquier orden
2. Modifica los campos necesarios
3. Puedes cambiar el cliente y el vehículo
4. Actualiza el estado de la orden
5. Haz clic en "Guardar"

#### Ver Detalles
- Haz clic en "Detalles" para ver información completa de:
  - Cliente
  - Vehículo
  - Orden de servicio

#### Eliminar Orden
1. Haz clic en "Eliminar"
2. Confirma la eliminación

## Tecnologías Utilizadas

- **Backend**: ASP.NET Core 8.0
- **Frontend**: Razor Pages, Bootstrap 5, jQuery
- **ORM**: Entity Framework Core 8.0
- **Base de Datos**: SQL Server
- **Arquitectura**: Razor Pages con patrón Repository implícito

## Estructura de Carpetas

- `Models/`: Clases de entidades que representan las tablas de la base de datos
- `Data/`: DbContext de Entity Framework Core
- `Pages/ServiceOrders/`: Páginas Razor para CRUD de órdenes
- `wwwroot/`: Archivos estáticos (CSS, JS, imágenes)

## Características de Seguridad

- Validación del lado del servidor
- Validación del lado del cliente con jQuery Validation
- Prevención de SQL Injection mediante EF Core
- Token Anti-Forgery en formularios

## Próximas Funcionalidades

- CRUD de Clientes
- CRUD de Vehículos
- CRUD de Servicios
- CRUD de Refacciones
- CRUD de Mecánicos
- Asignación de servicios a órdenes
- Asignación de refacciones a órdenes
- Asignación de mecánicos a órdenes
- Reportes y estadísticas
- Dashboard con métricas

## Notas Importantes

- El sistema está configurado para usar SQL Server con autenticación de Windows
- La base de datos debe crearse antes de ejecutar la aplicación
- Los triggers de la base de datos calculan automáticamente la antigüedad de los vehículos
- El estado de la orden usa valores específicos: "Abierta", "En proceso", "Finalizada", "Cancelada"

## Soporte

Para problemas o preguntas sobre el sistema, consulta la documentación de:
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Razor Pages](https://docs.microsoft.com/aspnet/core/razor-pages)
