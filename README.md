# API de atención al cliente en tiempo real

API REST para gestionar clientes, empleados y turnos. Usa ASP.NET Core, Entity Framework Core, SQL Server y SignalR para enviar notificaciones en tiempo real.

## Requisitos

- .NET SDK 10.
- Docker Desktop en ejecución.

## Configuración y ejecución

Desde la carpeta del repositorio:

```bash
cp .env.example .env
```

Cambia la contraseña de ejemplo en las dos variables de `.env`. Usa esa misma contraseña en lugar de `TU_CLAVE`:

```bash
docker compose up -d --wait
dotnet restore
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost,14337;Database=AtencionClientes;User Id=sa;Password=TU_CLAVE;TrustServerCertificate=True" --project src/AtencionClientes.Api
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project src/AtencionClientes.Api
dotnet run --project src/AtencionClientes.Api --launch-profile http
```

En Rider también puedes abrir `ApiAtencionClientes.sln` y ejecutar la API con el perfil `http`, después de configurar la base de datos.

## Probar la API

Abre [Swagger](http://localhost:5180/swagger). Permite registrar y consultar clientes y empleados, crear turnos, asignarlos, finalizarlos y cancelarlos.

## Recibir notificaciones

Primero registra los clientes desde Swagger. Luego abre una terminal para cada conexión:

```bash
dotnet run --project src/AtencionClientes.Cliente -- empleado
dotnet run --project src/AtencionClientes.Cliente -- cliente 1
dotnet run --project src/AtencionClientes.Cliente -- cliente 2
```

Reemplaza `1` y `2` por los ID reales de los clientes registrados. El Hub está en `/hubs/turnos` y las consolas muestran los avisos mientras realizas las operaciones desde Swagger.
