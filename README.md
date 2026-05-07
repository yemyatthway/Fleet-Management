# Fleet Management

Fleet Management is a Vue 3 + Vuetify frontend with an ASP.NET Core API backend and SQL Server storage. The app covers fleet operations such as vehicles, trips, maintenance, incidents, expenses, inventory, reports, users, roles, permissions, audit logs, and dashboard summaries.

## Stack

- Frontend: Vue 3, Vite, Vue Router, Vuetify, Material Design Icons
- Backend: ASP.NET Core 8 minimal APIs, Entity Framework Core, SQL Server
- Auth: JWT login with role-based permissions
- Email: SMTP support for OTP and trip assignment/update emails
- Storage: SQL Server plus local upload folders under `backend/FleetManagement.Api/wwwroot/uploads`

## Main Features

- Role-based login for Admin, Dispatcher, Driver, and Mechanic
- Permission matrix with `CanView`, `CanCreate`, `CanEdit`, and `CanDelete`
- Backend-connected pages for vehicles, trips, maintenance, incidents, expenses, inventory, users, roles, setup pages, reports, audit logs, and dashboard data
- Driver, dispatcher, and mechanic work scopes with `My Work` and `All Work` views
- Trip assignment emails and update emails
- Trip load validation against vehicle capacity
- Vehicle-driver sync in trip forms:
  - choosing a vehicle auto-fills its assigned driver
  - choosing a driver auto-fills that driver's assigned vehicle
- Shared setup pages for status, trip type, cargo type, priority, incident type, severity, expense type, maintenance type, document type, supplier, departments, locations, vehicle types, and fuel types
- Report export to PDF and Excel-compatible files
- Audit logs and status history

## Prerequisites

- .NET 8 SDK
- Node.js 20+ recommended
- SQL Server running locally on port `1433`
- A SQL Server database user matching the connection string, or update the connection string in appsettings

Default connection string:

```json
"Server=localhost,1433;Database=FleetManagementDb;User Id=Username;Password=YourStrongPsw;TrustServerCertificate=True;Encrypt=False"
```

## Run Backend

```bash
cd backend/FleetManagement.Api
dotnet restore
dotnet run
```

The API runs at:

```text
http://localhost:5215
```

If `http://127.0.0.1:5215` is already in use, stop the existing API process or run the API on another port.

## Run Frontend

```bash
cd frontend
npm install
npm run dev
```

Vite prints the local frontend URL in the terminal, usually:

```text
http://localhost:5173
```

## Seed Data

Seed behavior is controlled by:

```json
"SeedData": {
  "Enabled": false
}
```

Set it to `true` when you want demo/test data created or repaired:

```json
"SeedData": {
  "Enabled": true
}
```

Current seed behavior:

- Seeds fixed roles: Admin, Dispatcher, Driver, Mechanic
- Seeds demo users when enabled
- Seeds setup options when enabled
- Adds missing setup options without overwriting existing setup rows
- Repairs demo data relationships so seeded trips, vehicles, incidents, and expenses stay consistent
- Keeps trip load weight and volume below vehicle capacity for demo trips

Recommended workflow:

1. Turn seed on only when you need demo data.
2. Start the backend once.
3. Turn seed off again so user edits are not reset on every restart.

Default seeded password:

```text
Password@123
```

## Auth and Session

Login uses JWT. The frontend stores the authenticated session locally so API requests can include the bearer token and user context headers.

JWT secret is configured under:

```json
"Jwt": {
  "Secret": "change-this-before-production"
}
```

Use a long private value for real deployments.

## Email Setup

SMTP settings live under `Smtp` in appsettings.

For Gmail SMTP, use a Gmail App Password, not your normal Gmail password:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "EnableSsl": "true",
  "UserName": "your-email@gmail.com",
  "Password": "your-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "FleetManager"
}
```

Do not commit real SMTP credentials to source control. Prefer local development settings, environment variables, or user secrets.

## Uploads

Uploaded user, vehicle, inventory, and related images are stored locally under:

```text
backend/FleetManagement.Api/wwwroot/uploads
```

When supported records or image fields are removed, backend cleanup removes the related local upload files.

## Useful API Areas

- Auth: `/api/auth/login`, `/api/auth/verify-otp`
- Profile: `/api/profile`, `/api/profile/change-password`
- Dashboard: `/api/dashboard/summary`
- Reports: `/api/reports/{reportType}`
- Vehicles: `/api/vehicles`
- Trips: `/api/trips`
- Maintenance: `/api/maintenance-tickets`
- Inventory: `/api/inventory-parts`
- Incidents: `/api/incidents`
- Expenses: `/api/expenses`
- Users: `/api/users`
- Roles: `/api/roles`
- Permissions: `/api/permissions`
- Audit: `/api/audit-logs`, `/api/status-history`

## Build Checks

Backend:

```bash
cd backend/FleetManagement.Api
dotnet build
```

Frontend:

```bash
cd frontend
npm run build
```

Known build warning:

- `NU1900` can appear when NuGet vulnerability metadata cannot be fetched from `https://api.nuget.org`. This is a network/package metadata warning, not a code compile failure.

Vite may also warn about large chunks. The app still builds; code splitting can be improved later if needed.
