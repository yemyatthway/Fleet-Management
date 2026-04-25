# Fleet Management

This repository runs as a Vue 3 + Vite frontend with a Roles-only ASP.NET Core backend. Users, departments, locations, and the rest of the app still use local fake data in the frontend, but the Roles page now talks to SQL Server through the backend API.

## Run the app

```bash
dotnet restore backend/FleetManagement.Api/FleetManagement.Api.csproj
dotnet run --project backend/FleetManagement.Api
cd frontend
npm run dev
```

## Notes

- Roles page uses `http://localhost:5215/api/roles`.
- Start your SQL Server container yourself in Docker Desktop, then run the API.
- The backend seeds roles and members on first startup.
- Other pages still use frontend-local fake data.
