# FleetManagement.Api

ASP.NET Core 8 Web API for the Fleet Management frontend.

## Local SQL Server

On macOS, the easiest local MSSQL setup is SQL Server in Docker, managed from Azure Data Studio:

```bash
docker run \
  --name fleet-sql \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Use this connection in Azure Data Studio:

```text
Server: localhost,1433
User: sa
Password: YourStrong!Passw0rd
Trust server certificate: true
```

## Run API

```bash
dotnet run --project backend/FleetManagement.Api
```

In development, the API uses `EnsureCreatedAsync()` and seeds starter roles/users if the database is empty.

## Roles Endpoints

```http
GET    /api/roles
GET    /api/roles/{id}
GET    /api/roles/{id}/members
POST   /api/roles
PUT    /api/roles/{id}
DELETE /api/roles/{id}
```

The delete endpoint blocks removal when users are assigned to a role.
