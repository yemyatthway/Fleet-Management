using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public static class SchemaBootstrapper
{
  public static async Task EnsureRolesSchemaAsync(FleetDbContext db)
  {
    await db.Database.EnsureCreatedAsync();

    var hasCodeColumn = await ColumnExistsAsync(db, "Roles", "Code");
    if (!hasCodeColumn)
    {
      await db.Database.ExecuteSqlRawAsync("ALTER TABLE Roles ADD Code nvarchar(20) NULL;");
    }

    var hasIsDeletedColumn = await ColumnExistsAsync(db, "Roles", "IsDeleted");
    if (!hasIsDeletedColumn)
    {
      await db.Database.ExecuteSqlRawAsync("ALTER TABLE Roles ADD IsDeleted int NOT NULL CONSTRAINT DF_Roles_IsDeleted DEFAULT 0;");
    }

    if (!hasCodeColumn)
    {
      var roleIds = await db.Roles
        .AsNoTracking()
        .OrderBy(r => r.CreatedAt)
        .ThenBy(r => r.Id)
        .Select(r => r.Id)
        .ToListAsync();

      for (var index = 0; index < roleIds.Count; index++)
      {
        var code = $"ROL-{index + 1:D4}";
        await db.Database.ExecuteSqlRawAsync(
          "UPDATE Roles SET Code = {0} WHERE Id = {1} AND (Code IS NULL OR Code = '')",
          code,
          roleIds[index]);
      }

      await db.Database.ExecuteSqlRawAsync("UPDATE Roles SET Code = CONCAT('ROL-', RIGHT(CONCAT('0000', ABS(CHECKSUM(NEWID())) % 10000), 4)) WHERE Code IS NULL OR Code = '';");
      await db.Database.ExecuteSqlRawAsync("ALTER TABLE Roles ALTER COLUMN Code nvarchar(20) NOT NULL;");
    }

    var hasCodeIndex = await IndexExistsAsync(db, "Roles", "IX_Roles_Code");
    if (!hasCodeIndex)
    {
      await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_Roles_Code ON Roles(Code);");
    }

    var hasRolePermissionsTable = await TableExistsAsync(db, "RolePermissions");
    if (!hasRolePermissionsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE RolePermissions (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          RoleId nvarchar(80) NOT NULL,
          ModuleKey nvarchar(80) NOT NULL,
          CanView bit NOT NULL,
          CanCreate bit NOT NULL,
          CanEdit bit NOT NULL,
          CanDelete bit NOT NULL,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL,
          CONSTRAINT FK_RolePermissions_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IX_RolePermissions_RoleId_ModuleKey ON RolePermissions(RoleId, ModuleKey);
        """
      );
    }

    var hasUsersTable = await TableExistsAsync(db, "Users");
    if (!hasUsersTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE Users (
          Id nvarchar(80) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          EmployeeId nvarchar(80) NOT NULL,
          NrcNumber nvarchar(80) NOT NULL,
          Email nvarchar(160) NOT NULL,
          PasswordHash nvarchar(128) NOT NULL CONSTRAINT DF_Users_PasswordHash DEFAULT '',
          RoleId nvarchar(80) NOT NULL,
          Status nvarchar(30) NOT NULL,
          Phone nvarchar(40) NOT NULL,
          Avatar nvarchar(500) NOT NULL,
          NrcFront nvarchar(500) NOT NULL,
          NrcBack nvarchar(500) NOT NULL,
          Department nvarchar(120) NOT NULL,
          Title nvarchar(120) NOT NULL,
          Location nvarchar(120) NOT NULL,
          Manager nvarchar(120) NOT NULL,
          LicenseNumber nvarchar(80) NULL,
          LicenseClass nvarchar(40) NULL,
          LicenseExpiry nvarchar(40) NULL,
          EmergencyContactName nvarchar(120) NOT NULL,
          EmergencyContactRelation nvarchar(80) NOT NULL,
          EmergencyContactPhone nvarchar(40) NOT NULL,
          Address nvarchar(255) NOT NULL,
          TwoFactorEnabled bit NOT NULL,
          Notes nvarchar(2000) NULL,
          JoinDate nvarchar(40) NOT NULL,
          LastLogin nvarchar(80) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL,
          CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
        );
        CREATE INDEX IX_Users_EmployeeId ON Users(EmployeeId);
        CREATE INDEX IX_Users_Email ON Users(Email);
        CREATE INDEX IX_Users_RoleId ON Users(RoleId);
        """
      );
    }
    else
    {
      var hasPasswordHashColumn = await ColumnExistsAsync(db, "Users", "PasswordHash");
      if (!hasPasswordHashColumn)
      {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD PasswordHash nvarchar(128) NOT NULL CONSTRAINT DF_Users_PasswordHash DEFAULT '';");
      }
    }

    var hasDepartmentsTable = await TableExistsAsync(db, "DepartmentCodeOptions");
    if (!hasDepartmentsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE DepartmentCodeOptions (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          Description nvarchar(500) NULL,
          Status nvarchar(20) NOT NULL,
          CreatedAt datetimeoffset NOT NULL,
          UpdatedAt datetimeoffset NULL
        );
        CREATE UNIQUE INDEX IX_DepartmentCodeOptions_Name ON DepartmentCodeOptions(Name);
        """
      );
    }
    else
    {
      var hasDepartmentNameIndex = await IndexExistsAsync(db, "DepartmentCodeOptions", "IX_DepartmentCodeOptions_Name");
      if (!hasDepartmentNameIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_DepartmentCodeOptions_Name ON DepartmentCodeOptions(Name);");
      }
    }

    var hasLocationsTable = await TableExistsAsync(db, "LocationCodeOptions");
    if (!hasLocationsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE LocationCodeOptions (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          Code nvarchar(40) NOT NULL,
          Type nvarchar(50) NOT NULL,
          Address nvarchar(300) NOT NULL,
          City nvarchar(120) NOT NULL,
          Country nvarchar(120) NOT NULL,
          ContactPerson nvarchar(120) NULL,
          Phone nvarchar(40) NOT NULL,
          OperatingHours nvarchar(80) NOT NULL,
          Notes nvarchar(500) NULL,
          Status nvarchar(20) NOT NULL,
          CreatedAt datetimeoffset NOT NULL,
          UpdatedAt datetimeoffset NULL
        );
        CREATE UNIQUE INDEX IX_LocationCodeOptions_Name ON LocationCodeOptions(Name);
        CREATE UNIQUE INDEX IX_LocationCodeOptions_Code ON LocationCodeOptions(Code);
        """
      );
    }
    else
    {
      var hasTypeColumn = await ColumnExistsAsync(db, "LocationCodeOptions", "Type");
      if (!hasTypeColumn)
      {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE LocationCodeOptions ADD Type nvarchar(50) NULL;");
        var hasLegacyType = await ColumnExistsAsync(db, "LocationCodeOptions", "LocationType");
        if (hasLegacyType)
        {
          await db.Database.ExecuteSqlRawAsync("UPDATE LocationCodeOptions SET Type = LocationType WHERE Type IS NULL;");
        }
        await db.Database.ExecuteSqlRawAsync("UPDATE LocationCodeOptions SET Type = 'Warehouse' WHERE Type IS NULL OR Type = '';");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE LocationCodeOptions ALTER COLUMN Type nvarchar(50) NOT NULL;");
      }

      var hasNotesColumn = await ColumnExistsAsync(db, "LocationCodeOptions", "Notes");
      if (!hasNotesColumn)
      {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE LocationCodeOptions ADD Notes nvarchar(500) NULL;");
        var hasLegacyDescription = await ColumnExistsAsync(db, "LocationCodeOptions", "Description");
        if (hasLegacyDescription)
        {
          await db.Database.ExecuteSqlRawAsync("UPDATE LocationCodeOptions SET Notes = Description WHERE Notes IS NULL;");
        }
      }

      var hasNameIndex = await IndexExistsAsync(db, "LocationCodeOptions", "IX_LocationCodeOptions_Name");
      if (!hasNameIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_LocationCodeOptions_Name ON LocationCodeOptions(Name);");
      }

      var hasLocationCodeIndex = await IndexExistsAsync(db, "LocationCodeOptions", "IX_LocationCodeOptions_Code");
      if (!hasLocationCodeIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_LocationCodeOptions_Code ON LocationCodeOptions(Code);");
      }
    }

    var hasLocationTypesTable = await TableExistsAsync(db, "LocationTypeCodeOptions");
    if (!hasLocationTypesTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE LocationTypeCodeOptions (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          Code nvarchar(40) NOT NULL,
          Description nvarchar(500) NULL,
          Status nvarchar(20) NOT NULL,
          CreatedAt datetimeoffset NOT NULL,
          UpdatedAt datetimeoffset NULL
        );
        CREATE UNIQUE INDEX IX_LocationTypeCodeOptions_Name ON LocationTypeCodeOptions(Name);
        CREATE UNIQUE INDEX IX_LocationTypeCodeOptions_Code ON LocationTypeCodeOptions(Code);
        """
      );
    }
    else
    {
      var hasLocationTypeNameIndex = await IndexExistsAsync(db, "LocationTypeCodeOptions", "IX_LocationTypeCodeOptions_Name");
      if (!hasLocationTypeNameIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_LocationTypeCodeOptions_Name ON LocationTypeCodeOptions(Name);");
      }

      var hasLocationTypeCodeIndex = await IndexExistsAsync(db, "LocationTypeCodeOptions", "IX_LocationTypeCodeOptions_Code");
      if (!hasLocationTypeCodeIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_LocationTypeCodeOptions_Code ON LocationTypeCodeOptions(Code);");
      }
    }

    var hasVehicleTypesTable = await TableExistsAsync(db, "VehicleTypeCodeOptions");
    if (!hasVehicleTypesTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE VehicleTypeCodeOptions (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          Code nvarchar(40) NOT NULL,
          Description nvarchar(500) NULL,
          Status nvarchar(20) NOT NULL,
          CreatedAt datetimeoffset NOT NULL,
          UpdatedAt datetimeoffset NULL
        );
        CREATE UNIQUE INDEX IX_VehicleTypeCodeOptions_Name ON VehicleTypeCodeOptions(Name);
        CREATE UNIQUE INDEX IX_VehicleTypeCodeOptions_Code ON VehicleTypeCodeOptions(Code);
        """
      );
    }
    else
    {
      var hasVehicleTypeNameIndex = await IndexExistsAsync(db, "VehicleTypeCodeOptions", "IX_VehicleTypeCodeOptions_Name");
      if (!hasVehicleTypeNameIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_VehicleTypeCodeOptions_Name ON VehicleTypeCodeOptions(Name);");
      }

      var hasVehicleTypeCodeIndex = await IndexExistsAsync(db, "VehicleTypeCodeOptions", "IX_VehicleTypeCodeOptions_Code");
      if (!hasVehicleTypeCodeIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_VehicleTypeCodeOptions_Code ON VehicleTypeCodeOptions(Code);");
      }
    }

    var hasFuelTypesTable = await TableExistsAsync(db, "FuelTypeCodeOptions");
    if (!hasFuelTypesTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE FuelTypeCodeOptions (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          Code nvarchar(40) NOT NULL,
          Description nvarchar(500) NULL,
          Status nvarchar(20) NOT NULL,
          CreatedAt datetimeoffset NOT NULL,
          UpdatedAt datetimeoffset NULL
        );
        CREATE UNIQUE INDEX IX_FuelTypeCodeOptions_Name ON FuelTypeCodeOptions(Name);
        CREATE UNIQUE INDEX IX_FuelTypeCodeOptions_Code ON FuelTypeCodeOptions(Code);
        """
      );
    }
    else
    {
      var hasFuelTypeNameIndex = await IndexExistsAsync(db, "FuelTypeCodeOptions", "IX_FuelTypeCodeOptions_Name");
      if (!hasFuelTypeNameIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_FuelTypeCodeOptions_Name ON FuelTypeCodeOptions(Name);");
      }

      var hasFuelTypeCodeIndex = await IndexExistsAsync(db, "FuelTypeCodeOptions", "IX_FuelTypeCodeOptions_Code");
      if (!hasFuelTypeCodeIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_FuelTypeCodeOptions_Code ON FuelTypeCodeOptions(Code);");
      }
    }

    var hasVehiclesTable = await TableExistsAsync(db, "Vehicles");
    if (!hasVehiclesTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE Vehicles (
          Id nvarchar(40) NOT NULL PRIMARY KEY,
          Plate nvarchar(40) NOT NULL,
          Region nvarchar(120) NOT NULL,
          Type nvarchar(120) NOT NULL,
          Model nvarchar(120) NOT NULL,
          Make nvarchar(120) NULL,
          Year nvarchar(20) NULL,
          Color nvarchar(80) NULL,
          Status nvarchar(30) NOT NULL,
          Ownership nvarchar(40) NULL,
          Driver nvarchar(120) NOT NULL,
          DriverImage nvarchar(500) NULL,
          Depot nvarchar(120) NULL,
          Capacity nvarchar(80) NULL,
          FuelCapacity nvarchar(80) NULL,
          FuelType nvarchar(80) NOT NULL,
          Vin nvarchar(120) NULL,
          EngineNo nvarchar(120) NULL,
          Odometer nvarchar(80) NULL,
          LastService nvarchar(40) NULL,
          NextService nvarchar(40) NULL,
          ServiceNote nvarchar(255) NULL,
          PurchaseCost nvarchar(80) NULL,
          RegistrationNo nvarchar(120) NULL,
          RegistrationExpiry nvarchar(40) NULL,
          RoadTaxExpiry nvarchar(40) NULL,
          InsuranceExpiry nvarchar(40) NULL,
          InsuranceProvider nvarchar(120) NULL,
          InsurancePolicy nvarchar(120) NULL,
          InspectionDue nvarchar(40) NULL,
          AcquiredDate nvarchar(40) NULL,
          Image nvarchar(500) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_Vehicles_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        CREATE UNIQUE INDEX IX_Vehicles_Plate ON Vehicles(Plate);
        """
      );
    }
    else
    {
      var hasVehiclesPlateIndex = await IndexExistsAsync(db, "Vehicles", "IX_Vehicles_Plate");
      if (!hasVehiclesPlateIndex)
      {
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_Vehicles_Plate ON Vehicles(Plate);");
      }
    }

    var hasMaintenanceTicketsTable = await TableExistsAsync(db, "MaintenanceTickets");
    if (!hasMaintenanceTicketsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE MaintenanceTickets (
          Id nvarchar(40) NOT NULL PRIMARY KEY,
          Vehicle nvarchar(120) NOT NULL,
          VehicleId nvarchar(80) NOT NULL,
          Issue nvarchar(160) NOT NULL,
          Details nvarchar(500) NOT NULL,
          ReportedDate nvarchar(40) NOT NULL,
          Mechanic nvarchar(120) NOT NULL,
          Status nvarchar(30) NOT NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_MaintenanceTickets_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        CREATE UNIQUE INDEX IX_MaintenanceTickets_Id ON MaintenanceTickets(Id);
        """
      );
    }

    var hasIncidentsTable = await TableExistsAsync(db, "Incidents");
    if (!hasIncidentsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE Incidents (
          Id nvarchar(40) NOT NULL PRIMARY KEY,
          VehicleId nvarchar(80) NOT NULL,
          Driver nvarchar(120) NOT NULL,
          Date nvarchar(40) NOT NULL,
          Type nvarchar(120) NOT NULL,
          Severity nvarchar(40) NOT NULL,
          Status nvarchar(40) NOT NULL,
          Cost nvarchar(80) NULL,
          Notes nvarchar(1000) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_Incidents_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        CREATE UNIQUE INDEX IX_Incidents_Id ON Incidents(Id);
        """
      );
    }

    var hasExpensesTable = await TableExistsAsync(db, "Expenses");
    if (!hasExpensesTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE Expenses (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          ExpenseDate nvarchar(40) NOT NULL,
          ExpenseType nvarchar(120) NOT NULL,
          VehicleId nvarchar(80) NULL,
          TripNumber nvarchar(80) NULL,
          DriverName nvarchar(120) NULL,
          Amount decimal(18,2) NOT NULL,
          Status nvarchar(40) NOT NULL,
          Notes nvarchar(1000) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_Expenses_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        """
      );
    }

    var hasFleetDocumentsTable = await TableExistsAsync(db, "FleetDocuments");
    if (!hasFleetDocumentsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE FleetDocuments (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          OwnerType nvarchar(40) NOT NULL,
          OwnerId nvarchar(80) NOT NULL,
          OwnerName nvarchar(160) NOT NULL,
          DocumentType nvarchar(120) NOT NULL,
          DocumentNumber nvarchar(120) NULL,
          IssueDate nvarchar(40) NULL,
          ExpiryDate nvarchar(40) NULL,
          Status nvarchar(40) NOT NULL,
          Notes nvarchar(1000) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_FleetDocuments_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        """
      );
    }

    var hasAuditLogsTable = await TableExistsAsync(db, "AuditLogs");
    if (!hasAuditLogsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE AuditLogs (
          Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
          RoleId nvarchar(80) NOT NULL,
          ModuleKey nvarchar(80) NOT NULL,
          Action nvarchar(40) NOT NULL,
          EntityId nvarchar(80) NOT NULL,
          Description nvarchar(1000) NOT NULL,
          CreatedAt datetime2 NOT NULL
        );
        """
      );
    }

    var hasStatusHistoriesTable = await TableExistsAsync(db, "StatusHistories");
    if (!hasStatusHistoriesTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE StatusHistories (
          Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
          EntityType nvarchar(80) NOT NULL,
          EntityId nvarchar(80) NOT NULL,
          OldStatus nvarchar(80) NULL,
          NewStatus nvarchar(80) NOT NULL,
          RoleId nvarchar(80) NOT NULL,
          CreatedAt datetime2 NOT NULL
        );
        """
      );
    }

    var hasInventoryPartsTable = await TableExistsAsync(db, "InventoryParts");
    if (!hasInventoryPartsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE InventoryParts (
          Id nvarchar(40) NOT NULL PRIMARY KEY,
          Name nvarchar(160) NOT NULL,
          PartNo nvarchar(80) NOT NULL,
          Category nvarchar(120) NOT NULL,
          Stock int NOT NULL,
          ReorderPoint int NOT NULL,
          Supplier nvarchar(160) NULL,
          UnitCost nvarchar(80) NULL,
          Location nvarchar(160) NULL,
          Image nvarchar(500) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_InventoryParts_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        CREATE UNIQUE INDEX IX_InventoryParts_Id ON InventoryParts(Id);
        CREATE INDEX IX_InventoryParts_PartNo ON InventoryParts(PartNo);
        """
      );
    }
    else
    {
      var hasImageColumn = await ColumnExistsAsync(db, "InventoryParts", "Image");
      if (!hasImageColumn)
      {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE InventoryParts ADD Image nvarchar(500) NULL;");
      }
    }

    await EnsureTripSetupTableAsync(db, "TripTypeCodeOptions");
    await EnsureTripSetupTableAsync(db, "CargoTypeCodeOptions");
    await EnsureTripSetupTableAsync(db, "StatusCodeOptions");
    await EnsureTripSetupTableAsync(db, "TripPriorityCodeOptions");
    await EnsureTripSetupTableAsync(db, "IncidentTypeCodeOptions");
    await EnsureTripSetupTableAsync(db, "SeverityCodeOptions");
    await EnsureTripSetupTableAsync(db, "ExpenseTypeCodeOptions");
    await EnsureTripSetupTableAsync(db, "MaintenanceTypeCodeOptions");
    await EnsureTripSetupTableAsync(db, "DocumentTypeCodeOptions");
    await EnsureTripSetupTableAsync(db, "SupplierCodeOptions");

    var hasTripsTable = await TableExistsAsync(db, "Trips");
    if (!hasTripsTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE Trips (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          TripNumber nvarchar(40) NOT NULL,
          TripType nvarchar(80) NOT NULL,
          Status nvarchar(40) NOT NULL,
          Priority nvarchar(40) NOT NULL,
          CustomerName nvarchar(160) NOT NULL,
          Department nvarchar(120) NOT NULL,
          CostCenter nvarchar(80) NULL,
          VehicleId nvarchar(40) NOT NULL,
          VehiclePlate nvarchar(40) NOT NULL,
          TrailerNumber nvarchar(80) NULL,
          DriverName nvarchar(120) NOT NULL,
          CoDriverName nvarchar(120) NULL,
          DispatcherName nvarchar(120) NOT NULL,
          CargoType nvarchar(120) NOT NULL,
          LoadWeightKg decimal(18,2) NOT NULL,
          LoadVolumeM3 decimal(18,2) NOT NULL,
          PickupLocation nvarchar(160) NOT NULL,
          DropoffLocation nvarchar(160) NOT NULL,
          PickupContact nvarchar(160) NULL,
          DropoffContact nvarchar(160) NULL,
          DepartureDateTime nvarchar(40) NOT NULL,
          EstimatedArrival nvarchar(40) NOT NULL,
          ActualArrival nvarchar(40) NULL,
          PlannedDistanceKm decimal(18,2) NOT NULL,
          StartingOdometerKm decimal(18,2) NOT NULL,
          CurrentOdometerKm decimal(18,2) NOT NULL,
          EndingOdometerKm decimal(18,2) NULL,
          FuelIssuedLiters decimal(18,2) NOT NULL,
          TollEstimate decimal(18,2) NOT NULL,
          PermitRequired bit NOT NULL,
          TemperatureControlled bit NOT NULL,
          TemperatureRange nvarchar(80) NULL,
          SpecialInstructions nvarchar(1000) NULL,
          DriverNotes nvarchar(1000) NULL,
          IsDeleted int NOT NULL CONSTRAINT DF_Trips_IsDeleted DEFAULT 0,
          CreatedAt datetime2 NOT NULL,
          UpdatedAt datetime2 NOT NULL
        );
        CREATE UNIQUE INDEX IX_Trips_TripNumber ON Trips(TripNumber);
        """
      );
    }
  }

  private static async Task EnsureTripSetupTableAsync(FleetDbContext db, string tableName)
  {
    var hasTable = await TableExistsAsync(db, tableName);
    if (!hasTable)
    {
      await db.Database.ExecuteSqlRawAsync(
        $"""
        CREATE TABLE {tableName} (
          Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          Name nvarchar(120) NOT NULL,
          Code nvarchar(40) NOT NULL,
          Description nvarchar(500) NULL,
          Status nvarchar(20) NOT NULL,
          CreatedAt datetimeoffset NOT NULL,
          UpdatedAt datetimeoffset NULL
        );
        CREATE UNIQUE INDEX IX_{tableName}_Name ON {tableName}(Name);
        CREATE UNIQUE INDEX IX_{tableName}_Code ON {tableName}(Code);
        """
      );
      return;
    }

    var hasNameIndex = await IndexExistsAsync(db, tableName, $"IX_{tableName}_Name");
    if (!hasNameIndex)
    {
      await db.Database.ExecuteSqlRawAsync($"CREATE UNIQUE INDEX IX_{tableName}_Name ON {tableName}(Name);");
    }

    var hasCodeIndex = await IndexExistsAsync(db, tableName, $"IX_{tableName}_Code");
    if (!hasCodeIndex)
    {
      await db.Database.ExecuteSqlRawAsync($"CREATE UNIQUE INDEX IX_{tableName}_Code ON {tableName}(Code);");
    }
  }

  private static async Task<bool> TableExistsAsync(FleetDbContext db, string tableName)
  {
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose) await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
      SELECT COUNT(*)
      FROM INFORMATION_SCHEMA.TABLES
      WHERE TABLE_NAME = @tableName
      """;
    command.Parameters.Add(new SqlParameter("@tableName", tableName));

    var result = await command.ExecuteScalarAsync();
    if (shouldClose) await connection.CloseAsync();
    return Convert.ToInt32(result) > 0;
  }

  private static async Task<bool> ColumnExistsAsync(FleetDbContext db, string tableName, string columnName)
  {
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose) await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
      SELECT COUNT(*)
      FROM INFORMATION_SCHEMA.COLUMNS
      WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName
      """;

    command.Parameters.Add(new SqlParameter("@tableName", tableName));
    command.Parameters.Add(new SqlParameter("@columnName", columnName));

    var result = await command.ExecuteScalarAsync();
    if (shouldClose) await connection.CloseAsync();
    return Convert.ToInt32(result) > 0;
  }

  private static async Task<bool> IndexExistsAsync(FleetDbContext db, string tableName, string indexName)
  {
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose) await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
      SELECT COUNT(*)
      FROM sys.indexes i
      INNER JOIN sys.objects o ON i.object_id = o.object_id
      WHERE o.name = @tableName AND i.name = @indexName
      """;

    command.Parameters.Add(new SqlParameter("@tableName", tableName));
    command.Parameters.Add(new SqlParameter("@indexName", indexName));

    var result = await command.ExecuteScalarAsync();
    if (shouldClose) await connection.CloseAsync();
    return Convert.ToInt32(result) > 0;
  }
}
