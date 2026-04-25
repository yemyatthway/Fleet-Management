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
