namespace FleetManagement.Api.Security;

public static class PermissionModules
{
  public static IReadOnlyList<PermissionModuleDefinition> All { get; } =
  [
    new("dashboard", "Dashboard", "Overview"),
    new("vehicles", "Vehicle Management", "Fleet"),
    new("trips", "Trips", "Fleet"),
    new("maintenance-tickets", "Maintenance Tickets", "Maintenance"),
    new("inventory-parts", "Inventory & Parts", "Maintenance"),
    new("incidents", "Incidents", "Maintenance"),
    new("reports", "Reports", "Reports"),
    new("expenses", "Expenses", "Reports"),
    new("vehicle-documents", "Vehicle Documents", "Compliance"),
    new("driver-documents", "Driver Documents", "Compliance"),
    new("audit-logs", "Audit Logs", "Administration"),
    new("users", "Users", "Administration"),
    new("roles", "Roles", "Administration"),
    new("permissions", "Permissions", "Administration"),
    new("department-setup", "Department Setup", "Setup"),
    new("location-setup", "Location Setup", "Setup"),
    new("location-type-setup", "Location Type Setup", "Setup"),
    new("vehicle-type-setup", "Vehicle Type Setup", "Setup"),
    new("fuel-type-setup", "Fuel Type Setup", "Setup"),
    new("trip-type-setup", "Trip Type Setup", "Setup"),
    new("cargo-type-setup", "Cargo Type Setup", "Setup"),
    new("status-setup", "Status Setup", "Setup"),
    new("trip-priority-setup", "Trip Priority Setup", "Setup"),
    new("incident-type-setup", "Incident Type Setup", "Setup"),
    new("severity-setup", "Severity Setup", "Setup"),
    new("expense-type-setup", "Expense Type Setup", "Setup"),
    new("maintenance-type-setup", "Maintenance Type Setup", "Setup"),
    new("document-type-setup", "Document Type Setup", "Setup"),
    new("supplier-setup", "Supplier Setup", "Setup"),
    new("settings", "Settings", "Administration")
  ];
}

public record PermissionModuleDefinition(string Key, string Name, string Category);
