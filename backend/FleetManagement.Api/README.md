# FleetManagement.Api

ASP.NET Core 8 backend for the Fleet Management app.

## Run

```bash
dotnet restore
dotnet run
```

Default URL:

```text
http://localhost:5215
```

## Configuration

Main settings:

- `ConnectionStrings:FleetDatabase`: SQL Server connection
- `Jwt:Secret`: JWT signing secret
- `SeedData:Enabled`: controls demo data creation/repair
- `Smtp:*`: SMTP email settings

Use `appsettings.Development.json`, environment variables, or user secrets for local private values.

## Main Endpoint Groups

- Dashboard and reports
- Auth and OTP verification
- Users, profile, roles, and permissions
- Vehicles and trips
- Maintenance tickets and inventory parts
- Incidents and expenses
- Setup/code option pages
- Audit logs and status history

Endpoint registration is split across `Endpoints/*Endpoints.cs`. `Program.cs` only wires services, middleware, schema/bootstrap, seed data, and endpoint groups.

## Seed Data

Set:

```json
"SeedData": {
  "Enabled": true
}
```

to seed demo data. Turn it off after seeding if you want frontend edits to persist without being repaired/reset on every backend restart.

Default seeded password:

```text
Password@123
```

## Uploads

Local uploads are served from:

```text
wwwroot/uploads
```

Image cleanup is handled by backend delete/update flows where image fields are supported.

## Build

```bash
dotnet build
```

`NU1900` warnings can happen if NuGet vulnerability metadata is unavailable. The build is still valid when there are no compile errors.
