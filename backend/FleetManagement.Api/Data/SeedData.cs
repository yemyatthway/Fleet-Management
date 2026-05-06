namespace FleetManagement.Api.Data;

using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public static class SeedData
{
  public static readonly string[] FixedRoleIds = ["admin", "dispatcher", "driver", "mechanic"];
  private const int DemoRecordTarget = 30;

  public static async Task InitializeAsync(FleetDbContext db)
  {
    var now = DateTime.UtcNow;
    var fixedRoles = new List<Models.Role>
    {
      new() { Id = "admin", Code = "ROL-0001", Name = "Admin", Description = "Full platform access with governance controls.", Status = "Active", IsDeleted = 0, CreatedAt = now, UpdatedAt = now },
      new() { Id = "dispatcher", Code = "ROL-0002", Name = "Dispatcher", Description = "Schedules routes, assigns drivers, and monitors trips.", Status = "Active", IsDeleted = 0, CreatedAt = now, UpdatedAt = now },
      new() { Id = "driver", Code = "ROL-0003", Name = "Driver", Description = "Executes assigned routes and updates trip status.", Status = "Active", IsDeleted = 0, CreatedAt = now, UpdatedAt = now },
      new() { Id = "mechanic", Code = "ROL-0004", Name = "Mechanic", Description = "Manages inspections, repairs, and maintenance logs.", Status = "Active", IsDeleted = 0, CreatedAt = now, UpdatedAt = now }
    };

    var existingRoles = await db.Roles.ToListAsync();
    foreach (var fixedRole in fixedRoles)
    {
      var existingRole = existingRoles.FirstOrDefault(role => role.Id == fixedRole.Id);
      if (existingRole is null)
      {
        db.Roles.Add(fixedRole);
        continue;
      }

      existingRole.Code = fixedRole.Code;
      existingRole.Name = fixedRole.Name;
      existingRole.Description = fixedRole.Description;
      existingRole.Status = "Active";
      existingRole.IsDeleted = 0;
      existingRole.UpdatedAt = now;
    }

    foreach (var role in existingRoles.Where(role => !FixedRoleIds.Contains(role.Id, StringComparer.OrdinalIgnoreCase)))
    {
      role.IsDeleted = 1;
      role.UpdatedAt = now;
    }

    await db.SaveChangesAsync();

    await SeedSetupDataAsync(db, now);
    await SeedPermissionsAsync(db, now);
    await SeedSystemUsersAsync(db, now);
    await SeedDemoRecordsAsync(db, now);
  }

  public static string HashPassword(string password)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes);
  }

  public static bool VerifyPassword(string password, string passwordHash) =>
    string.Equals(HashPassword(password), passwordHash, StringComparison.OrdinalIgnoreCase);

  private static async Task SeedSystemUsersAsync(FleetDbContext db, DateTime now)
  {
    var users = new List<User>
    {
      BuildSeedUser("seed-admin", "Admin User", "admin@fleet.com", "admin", "System Administrator", "Administration", now),
      BuildSeedUser("seed-dispatcher", "Dispatcher User", "dispatcher@fleet.com", "dispatcher", "Dispatcher", "Operations", now),
      BuildSeedUser("seed-driver", "Driver User", "driver@fleet.com", "driver", "Driver", "Transport", now, "DRV-0001", "B", "2027-12-31"),
      BuildSeedUser("seed-driver-2", "Aung Min", "aung.driver@fleet.com", "driver", "Driver", "Transport", now, "DRV-0002", "B", "2028-06-30"),
      BuildSeedUser("seed-mechanic", "Mechanic User", "mechanic@fleet.com", "mechanic", "Mechanic", "Maintenance", now),
      BuildSeedUser("seed-mechanic-2", "Myo Zaw", "myo.mechanic@fleet.com", "mechanic", "Senior Mechanic", "Maintenance", now)
    };

    var existingUsers = await db.Users.ToListAsync();
    foreach (var seedUser in users)
    {
      var existingUser = existingUsers.FirstOrDefault(user => user.Id == seedUser.Id);
      if (existingUser is null)
      {
        db.Users.Add(seedUser);
        continue;
      }

      existingUser.Name = seedUser.Name;
      existingUser.EmployeeId = seedUser.EmployeeId;
      existingUser.NrcNumber = seedUser.NrcNumber;
      existingUser.Email = seedUser.Email;
      existingUser.RoleId = seedUser.RoleId;
      existingUser.Status = "Active";
      existingUser.Phone = seedUser.Phone;
      existingUser.Department = seedUser.Department;
      existingUser.Title = seedUser.Title;
      existingUser.Location = seedUser.Location;
      existingUser.Manager = seedUser.Manager;
      existingUser.LicenseNumber = seedUser.LicenseNumber;
      existingUser.LicenseClass = seedUser.LicenseClass;
      existingUser.LicenseExpiry = seedUser.LicenseExpiry;
      existingUser.EmergencyContactName = seedUser.EmergencyContactName;
      existingUser.EmergencyContactRelation = seedUser.EmergencyContactRelation;
      existingUser.EmergencyContactPhone = seedUser.EmergencyContactPhone;
      existingUser.Address = seedUser.Address;
      existingUser.TwoFactorEnabled = seedUser.TwoFactorEnabled;
      existingUser.Notes = seedUser.Notes;
      existingUser.IsDeleted = 0;
      existingUser.UpdatedAt = now;
    }

    await db.SaveChangesAsync();
  }

  private static User BuildSeedUser(
    string id,
    string name,
    string email,
    string roleId,
    string title,
    string department,
    DateTime now,
    string? licenseNumber = null,
    string? licenseClass = null,
    string? licenseExpiry = null) =>
    new()
    {
      Id = id,
      Name = name,
      EmployeeId = $"EMP-{roleId.ToUpperInvariant()}",
      NrcNumber = roleId switch
      {
        "admin" => "9/SEED/000001",
        "dispatcher" => "9/SEED/000002",
        "driver" => "9/SEED/000003",
        "mechanic" => "9/SEED/000004",
        _ => "9/SEED/000000"
      },
      Email = email,
      PasswordHash = HashPassword("Password@123"),
      RoleId = roleId,
      Status = "Active",
      Phone = "09-000000000",
      Avatar = string.Empty,
      NrcFront = string.Empty,
      NrcBack = string.Empty,
      Department = department,
      Title = title,
      Location = "Main Office",
      Manager = "Admin User",
      LicenseNumber = licenseNumber,
      LicenseClass = licenseClass,
      LicenseExpiry = licenseExpiry,
      EmergencyContactName = "Emergency Contact",
      EmergencyContactRelation = "Family",
      EmergencyContactPhone = "09-111111111",
      Address = "Fleet Management Office",
      TwoFactorEnabled = false,
      Notes = "Seeded login account.",
      JoinDate = now.ToString("yyyy-MM-dd"),
      LastLogin = null,
      IsDeleted = 0,
      CreatedAt = now,
      UpdatedAt = now
    };

  private static async Task SeedSetupDataAsync(FleetDbContext db, DateTime now)
  {
    var offsetNow = DateTimeOffset.UtcNow;

    if (!await db.DepartmentCodeOptions.AnyAsync())
    {
      db.DepartmentCodeOptions.AddRange(
        new DepartmentCodeOption { Name = "Administration", Description = "System administration and governance.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new DepartmentCodeOption { Name = "Operations", Description = "Fleet planning and dispatch.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new DepartmentCodeOption { Name = "Transport", Description = "Drivers and daily transport execution.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new DepartmentCodeOption { Name = "Maintenance", Description = "Repairs, service, and inventory.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new DepartmentCodeOption { Name = "Finance", Description = "Expenses, insurance, and cost control.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow });
    }

    if (!await db.LocationTypeCodeOptions.AnyAsync())
    {
      db.LocationTypeCodeOptions.AddRange(
        new LocationTypeCodeOption { Name = "Office", Code = "OFFICE", Description = "Administrative office location.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new LocationTypeCodeOption { Name = "Depot", Code = "DEPOT", Description = "Vehicle depot or yard.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new LocationTypeCodeOption { Name = "Warehouse", Code = "WAREHOUSE", Description = "Parts and cargo storage.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new LocationTypeCodeOption { Name = "Workshop", Code = "WORKSHOP", Description = "Maintenance workshop.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow });
    }

    if (!await db.LocationCodeOptions.AnyAsync())
    {
      db.LocationCodeOptions.AddRange(
        new LocationCodeOption { Name = "Main Office", Code = "LOC-001", Type = "Office", Address = "No. 10 Fleet Road", City = "Yangon", Country = "Myanmar", ContactPerson = "Admin User", Phone = "09-100000001", OperatingHours = "09:00-18:00", Notes = "Main administration office.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new LocationCodeOption { Name = "Yangon East Yard", Code = "LOC-002", Type = "Depot", Address = "East Industrial Zone", City = "Yangon", Country = "Myanmar", ContactPerson = "Dispatcher User", Phone = "09-100000002", OperatingHours = "24/7", Notes = "Primary vehicle yard.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new LocationCodeOption { Name = "Mandalay Depot", Code = "LOC-003", Type = "Depot", Address = "Mandalay Logistics Park", City = "Mandalay", Country = "Myanmar", ContactPerson = "Aung Min", Phone = "09-100000003", OperatingHours = "08:00-20:00", Notes = "Upper Myanmar dispatch point.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new LocationCodeOption { Name = "Central Workshop", Code = "LOC-004", Type = "Workshop", Address = "Workshop Compound", City = "Yangon", Country = "Myanmar", ContactPerson = "Mechanic User", Phone = "09-100000004", OperatingHours = "08:00-18:00", Notes = "Service and repair center.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow });
    }

    if (!await db.VehicleTypeCodeOptions.AnyAsync())
    {
      db.VehicleTypeCodeOptions.AddRange(
        new VehicleTypeCodeOption { Name = "Box Truck", Code = "BOX", Description = "Closed cargo truck.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new VehicleTypeCodeOption { Name = "Delivery Van", Code = "VAN", Description = "Urban delivery vehicle.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new VehicleTypeCodeOption { Name = "Reefer Truck", Code = "REEFER", Description = "Temperature-controlled truck.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new VehicleTypeCodeOption { Name = "Tanker", Code = "TANKER", Description = "Liquid cargo tanker.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow });
    }

    if (!await db.FuelTypeCodeOptions.AnyAsync())
    {
      db.FuelTypeCodeOptions.AddRange(
        new FuelTypeCodeOption { Name = "Diesel", Code = "DSL", Description = "Diesel fuel.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new FuelTypeCodeOption { Name = "Petrol", Code = "PTR", Description = "Petrol fuel.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new FuelTypeCodeOption { Name = "CNG", Code = "CNG", Description = "Compressed natural gas.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow },
        new FuelTypeCodeOption { Name = "Electric", Code = "EV", Description = "Electric vehicle.", Status = "Active", CreatedAt = offsetNow, UpdatedAt = offsetNow });
    }

    await SeedTripSetupAsync(db.TripTypeCodeOptions, [
      ("Delivery", "TRIP-DEL", "Point-to-point delivery"),
      ("Line Haul", "TRIP-LH", "Long-distance cargo route"),
      ("Pickup", "TRIP-PU", "Pickup from customer or depot")
    ], offsetNow);
    await SeedTripSetupAsync(db.CargoTypeCodeOptions, [
      ("General Cargo", "CG-GEN", "Standard cargo"),
      ("Cold Chain", "CG-COLD", "Temperature-sensitive cargo"),
      ("Hazardous", "CG-HAZ", "Controlled hazardous goods")
    ], offsetNow);
    await SeedTripSetupAsync(db.StatusCodeOptions, [
      ("Active", "ST-ACT", "Active record"),
      ("Pending", "ST-PEN", "Waiting for action"),
      ("In Transit", "ST-TRANSIT", "Trip in progress"),
      ("Completed", "ST-COMP", "Completed workflow"),
      ("Maintenance", "ST-MAINT", "Maintenance state"),
      ("Disabled", "ST-DIS", "Disabled record")
    ], offsetNow);
    await SeedTripSetupAsync(db.TripPriorityCodeOptions, [
      ("Low", "PRI-LOW", "Low priority"),
      ("Medium", "PRI-MED", "Normal priority"),
      ("High", "PRI-HIGH", "High priority"),
      ("Critical", "PRI-CRIT", "Critical priority")
    ], offsetNow);
    await SeedTripSetupAsync(db.IncidentTypeCodeOptions, [
      ("Accident", "INC-ACC", "Road accident"),
      ("Breakdown", "INC-BRK", "Vehicle breakdown"),
      ("Damage", "INC-DMG", "Vehicle or cargo damage")
    ], offsetNow);
    await SeedTripSetupAsync(db.SeverityCodeOptions, [
      ("Low", "SEV-LOW", "Minor severity"),
      ("Medium", "SEV-MED", "Moderate severity"),
      ("High", "SEV-HIGH", "High severity"),
      ("Critical", "SEV-CRIT", "Critical severity")
    ], offsetNow);
    await SeedTripSetupAsync(db.ExpenseTypeCodeOptions, [
      ("Fuel", "EXP-FUEL", "Fuel cost"),
      ("Toll", "EXP-TOLL", "Toll cost"),
      ("Repair", "EXP-REPAIR", "Repair cost"),
      ("Parking", "EXP-PARK", "Parking cost")
    ], offsetNow);
    await SeedTripSetupAsync(db.MaintenanceTypeCodeOptions, [
      ("Preventive", "MNT-PREV", "Scheduled preventive service"),
      ("Corrective", "MNT-CORR", "Corrective repair"),
      ("Inspection", "MNT-INSP", "Inspection work"),
      ("Emergency Repair", "MNT-EMR", "Emergency repair")
    ], offsetNow);
    await SeedTripSetupAsync(db.DocumentTypeCodeOptions, [
      ("Registration", "DOC-REG", "Vehicle registration"),
      ("Insurance", "DOC-INS", "Insurance document"),
      ("Road Tax", "DOC-TAX", "Road tax document"),
      ("Driver License", "DOC-LIC", "Driver license"),
      ("NRC", "DOC-NRC", "NRC document")
    ], offsetNow);
    await SeedTripSetupAsync(db.SupplierCodeOptions, [
      ("Yangon Parts Co.", "SUP-YGN", "Parts supplier in Yangon"),
      ("Mandalay Auto Supply", "SUP-MDY", "Parts supplier in Mandalay"),
      ("Fleet Tire Service", "SUP-TIRE", "Tire and service supplier")
    ], offsetNow);

    await db.SaveChangesAsync();
  }

  private static async Task SeedPermissionsAsync(FleetDbContext db, DateTime now)
  {
    if (await db.RolePermissions.AnyAsync()) return;

    foreach (var roleId in FixedRoleIds)
    {
      foreach (var module in PermissionModules.All)
      {
        var defaultPermission = PermissionChecks.GetDefaultPermission(roleId, module.Key);
        db.RolePermissions.Add(new RolePermission
        {
          RoleId = roleId,
          ModuleKey = module.Key,
          CanView = defaultPermission.CanView,
          CanCreate = defaultPermission.CanCreate,
          CanEdit = defaultPermission.CanEdit,
          CanDelete = defaultPermission.CanDelete,
          CreatedAt = now,
          UpdatedAt = now
        });
      }
    }

    await db.SaveChangesAsync();
  }

  private static async Task SeedDemoRecordsAsync(FleetDbContext db, DateTime now)
  {
    if (!await db.Vehicles.AnyAsync())
    {
      db.Vehicles.AddRange(
        new Vehicle { Id = "VH-1001", Plate = "YGN-1187", Region = "Yangon", Type = "Box Truck", Model = "Isuzu FVR", Make = "Isuzu", Year = "2022", Color = "White", Status = "Active", Ownership = "Owned", Driver = "Driver User", DriverImage = string.Empty, Depot = "Yangon East Yard", Capacity = "6 tons", FuelCapacity = "120 L", FuelType = "Diesel", Vin = "MMTFVR20221001", EngineNo = "ENG-FVR-1001", Odometer = "24500", LastService = "2026-04-15", NextService = "2026-06-15", ServiceNote = "Routine oil and filter change.", PurchaseCost = "75000", RegistrationNo = "REG-YGN-1187", RegistrationExpiry = "2027-03-31", RoadTaxExpiry = "2027-03-31", InsuranceExpiry = "2027-04-30", InsuranceProvider = "Global Insurance", InsurancePolicy = "POL-1001", InspectionDue = "2026-08-01", AcquiredDate = "2024-01-10", Image = string.Empty, IsDeleted = 0, CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-2) },
        new Vehicle { Id = "VH-1002", Plate = "MDY-7742", Region = "Mandalay", Type = "Delivery Van", Model = "Toyota HiAce", Make = "Toyota", Year = "2021", Color = "Silver", Status = "Active", Ownership = "Leased", Driver = "Aung Min", DriverImage = string.Empty, Depot = "Mandalay Depot", Capacity = "1.5 tons", FuelCapacity = "70 L", FuelType = "Petrol", Vin = "MMTHIACE20211002", EngineNo = "ENG-HIA-1002", Odometer = "38600", LastService = "2026-04-20", NextService = "2026-06-20", ServiceNote = "Brake inspection completed.", PurchaseCost = "42000", RegistrationNo = "REG-MDY-7742", RegistrationExpiry = "2027-02-28", RoadTaxExpiry = "2027-02-28", InsuranceExpiry = "2027-05-15", InsuranceProvider = "Apex Insurance", InsurancePolicy = "POL-1002", InspectionDue = "2026-07-15", AcquiredDate = "2023-09-05", Image = string.Empty, IsDeleted = 0, CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-1) },
        new Vehicle { Id = "VH-1003", Plate = "YGN-4521", Region = "Yangon", Type = "Reefer Truck", Model = "Hino 500", Make = "Hino", Year = "2020", Color = "Blue", Status = "Maintenance", Ownership = "Owned", Driver = "Driver User", DriverImage = string.Empty, Depot = "Yangon East Yard", Capacity = "8 tons", FuelCapacity = "150 L", FuelType = "Diesel", Vin = "MMTHINO20201003", EngineNo = "ENG-HIN-1003", Odometer = "61200", LastService = "2026-03-30", NextService = "2026-05-30", ServiceNote = "Cooling unit requires inspection.", PurchaseCost = "98000", RegistrationNo = "REG-YGN-4521", RegistrationExpiry = "2027-01-31", RoadTaxExpiry = "2027-01-31", InsuranceExpiry = "2026-12-31", InsuranceProvider = "Global Insurance", InsurancePolicy = "POL-1003", InspectionDue = "2026-05-20", AcquiredDate = "2022-04-18", Image = string.Empty, IsDeleted = 0, CreatedAt = now.AddDays(-15), UpdatedAt = now });
    }

    if (!await db.Trips.AnyAsync())
    {
      db.Trips.AddRange(
        new Trip { TripNumber = "TRIP-1001", TripType = "Delivery", Status = "In Transit", Priority = "High", CustomerName = "Yangon Retail Group", Department = "Operations", CostCenter = "OPS-YGN", VehicleId = "VH-1001", VehiclePlate = "YGN-1187", TrailerNumber = null, DriverName = "Driver User", CoDriverName = "Aung Min", DispatcherName = "Dispatcher User", CargoType = "General Cargo", LoadWeightKg = 4200, LoadVolumeM3 = 18, PickupLocation = "Yangon East Yard", DropoffLocation = "Main Office", PickupContact = "09-200000001", DropoffContact = "09-200000002", DepartureDateTime = "2026-05-06T08:30", EstimatedArrival = "2026-05-06T14:30", ActualArrival = null, PlannedDistanceKm = 80, StartingOdometerKm = 24500, CurrentOdometerKm = 24548, EndingOdometerKm = null, FuelIssuedLiters = 40, TollEstimate = 12000, PermitRequired = false, TemperatureControlled = false, SpecialInstructions = "Call customer before arrival.", DriverNotes = "Traffic moderate.", IsDeleted = 0, CreatedAt = now.AddDays(-2), UpdatedAt = now },
        new Trip { TripNumber = "TRIP-1002", TripType = "Line Haul", Status = "Completed", Priority = "Medium", CustomerName = "Mandalay Wholesale", Department = "Operations", CostCenter = "OPS-MDY", VehicleId = "VH-1002", VehiclePlate = "MDY-7742", TrailerNumber = null, DriverName = "Aung Min", CoDriverName = null, DispatcherName = "Dispatcher User", CargoType = "General Cargo", LoadWeightKg = 1300, LoadVolumeM3 = 9, PickupLocation = "Mandalay Depot", DropoffLocation = "Yangon East Yard", PickupContact = "09-200000003", DropoffContact = "09-200000004", DepartureDateTime = "2026-05-04T07:00", EstimatedArrival = "2026-05-04T19:00", ActualArrival = "2026-05-04T18:40", PlannedDistanceKm = 620, StartingOdometerKm = 38200, CurrentOdometerKm = 38820, EndingOdometerKm = 38820, FuelIssuedLiters = 95, TollEstimate = 35000, PermitRequired = true, TemperatureControlled = false, SpecialInstructions = "Carry route permit.", DriverNotes = "Arrived early.", IsDeleted = 0, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-2) });
    }

    if (!await db.MaintenanceTickets.AnyAsync())
    {
      db.MaintenanceTickets.AddRange(
        new MaintenanceTicket { Id = "MT-2031", Vehicle = "Hino 500", VehicleId = "VH-1003", Issue = "Cooling unit inspection", Details = "Reefer temperature is unstable during long trips.", ReportedDate = "2026-05-05", Mechanic = "Mechanic User", Status = "Pending", IsDeleted = 0, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
        new MaintenanceTicket { Id = "MT-2032", Vehicle = "Isuzu FVR", VehicleId = "VH-1001", Issue = "Brake pad replacement", Details = "Front brake pads near minimum thickness.", ReportedDate = "2026-04-28", Mechanic = "Myo Zaw", Status = "Completed", IsDeleted = 0, CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-3) });
    }

    if (!await db.InventoryParts.AnyAsync())
    {
      db.InventoryParts.AddRange(
        new InventoryPart { Id = "PART-1001", Name = "Engine Oil 15W-40", PartNo = "OIL-1540", Category = "Fluids", Stock = 40, ReorderPoint = 12, Supplier = "Yangon Parts Co.", UnitCost = "28.50", Location = "Central Workshop", Image = string.Empty, IsDeleted = 0, CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-1) },
        new InventoryPart { Id = "PART-1002", Name = "Brake Pad Set", PartNo = "BRK-FVR-01", Category = "Brake", Stock = 8, ReorderPoint = 6, Supplier = "Mandalay Auto Supply", UnitCost = "65.00", Location = "Central Workshop", Image = string.Empty, IsDeleted = 0, CreatedAt = now.AddDays(-12), UpdatedAt = now.AddDays(-2) },
        new InventoryPart { Id = "PART-1003", Name = "Truck Tire 295/80R22.5", PartNo = "TIR-295", Category = "Tires", Stock = 5, ReorderPoint = 8, Supplier = "Fleet Tire Service", UnitCost = "210.00", Location = "Yangon East Yard", Image = string.Empty, IsDeleted = 0, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-1) });
    }

    if (!await db.Incidents.AnyAsync())
    {
      db.Incidents.AddRange(
        new Incident { Id = "INC-1001", VehicleId = "VH-1002", Driver = "Aung Min", Date = "2026-05-02", Type = "Damage", Severity = "Low", Status = "Completed", Cost = "150.00", Notes = "Minor bumper scratch at loading bay.", IsDeleted = 0, CreatedAt = now.AddDays(-4), UpdatedAt = now.AddDays(-2) },
        new Incident { Id = "INC-1002", VehicleId = "VH-1003", Driver = "Driver User", Date = "2026-05-05", Type = "Breakdown", Severity = "Medium", Status = "Pending", Cost = "0.00", Notes = "Cooling unit alarm triggered.", IsDeleted = 0, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) });
    }

    if (!await db.Expenses.AnyAsync())
    {
      db.Expenses.AddRange(
        new Expense { ExpenseDate = "2026-05-04", ExpenseType = "Fuel", VehicleId = "VH-1001", TripNumber = "TRIP-1001", DriverName = "Driver User", Amount = 185000, Status = "Active", Notes = "Diesel refill before trip.", IsDeleted = 0, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2) },
        new Expense { ExpenseDate = "2026-05-04", ExpenseType = "Toll", VehicleId = "VH-1002", TripNumber = "TRIP-1002", DriverName = "Aung Min", Amount = 35000, Status = "Active", Notes = "Expressway tolls.", IsDeleted = 0, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2) },
        new Expense { ExpenseDate = "2026-05-05", ExpenseType = "Repair", VehicleId = "VH-1003", TripNumber = null, DriverName = "Driver User", Amount = 240000, Status = "Pending", Notes = "Cooling unit diagnostic.", IsDeleted = 0, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) });
    }

    await db.SaveChangesAsync();
    await EnsureOperationalDemoRecordsAsync(db, now);

    if (!await db.AuditLogs.AnyAsync())
    {
      db.AuditLogs.Add(new AuditLog { RoleId = "admin", ModuleKey = "seed-data", Action = "Create", EntityId = "demo", Description = "Seeded demo records for testing.", CreatedAt = now });
    }

    if (!await db.StatusHistories.AnyAsync())
    {
      db.StatusHistories.AddRange(
        new StatusHistory { EntityType = "Vehicle", EntityId = "VH-1003", OldStatus = "Active", NewStatus = "Maintenance", RoleId = "admin", CreatedAt = now.AddDays(-1) },
        new StatusHistory { EntityType = "Trip", EntityId = "2", OldStatus = "In Transit", NewStatus = "Completed", RoleId = "dispatcher", CreatedAt = now.AddDays(-2) });
    }

    await db.SaveChangesAsync();
  }

  private static async Task EnsureOperationalDemoRecordsAsync(FleetDbContext db, DateTime now)
  {
    await EnsureDemoUsersAsync(db, now);
    await EnsureDemoVehiclesAsync(db, now);
    await db.SaveChangesAsync();

    await EnsureDemoTripsAsync(db, now);
    await EnsureDemoMaintenanceTicketsAsync(db, now);
    await EnsureDemoInventoryPartsAsync(db, now);
    await EnsureDemoIncidentsAsync(db, now);
    await EnsureDemoExpensesAsync(db, now);
    await EnsureDemoAuditLogsAsync(db, now);
    await EnsureDemoStatusHistoryAsync(db, now);
    await db.SaveChangesAsync();
  }

  private static async Task EnsureDemoUsersAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.Users.CountAsync(user => user.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var existingIds = new HashSet<string>(await db.Users.Select(user => user.Id).ToListAsync(), StringComparer.OrdinalIgnoreCase);
    var roleCycle = new[] { "dispatcher", "driver", "mechanic", "driver", "mechanic" };
    var firstNames = new[] { "Min", "Htet", "Nandar", "Kyaw", "Su", "Thiri", "Zaw", "Moe", "Hla", "Ei" };
    var lastNames = new[] { "Aung", "Naing", "Win", "Tun", "Oo", "Khaing", "Lwin", "Myint" };
    var added = 0;

    for (var index = 1; activeCount + added < DemoRecordTarget; index++)
    {
      var id = $"seed-demo-user-{index:D4}";
      if (!existingIds.Add(id)) continue;

      var roleId = roleCycle[index % roleCycle.Length];
      var name = $"{firstNames[index % firstNames.Length]} {lastNames[index % lastNames.Length]}";
      var title = roleId switch
      {
        "dispatcher" => "Dispatcher",
        "mechanic" => index % 2 == 0 ? "Mechanic" : "Senior Mechanic",
        _ => "Driver"
      };
      var department = roleId switch
      {
        "mechanic" => "Maintenance",
        "driver" => "Transport",
        _ => "Operations"
      };
      var user = BuildSeedUser(
        id,
        name,
        $"demo.{roleId}.{index:D4}@fleet.com",
        roleId,
        title,
        department,
        now.AddDays(-index),
        roleId == "driver" ? $"DRV-{3000 + index:D4}" : null,
        roleId == "driver" ? "B" : null,
        roleId == "driver" ? $"2028-{(index % 9) + 1:D2}-28" : null);

      user.EmployeeId = $"EMP-DEMO-{index:D4}";
      user.Phone = $"09-30{index:D7}";
      user.Location = index % 2 == 0 ? "Yangon East Yard" : "Mandalay Depot";
      user.CreatedAt = now.AddDays(-45 - index);
      user.UpdatedAt = now.AddDays(-(index % 9));
      db.Users.Add(user);
      added++;
    }
  }

  private static async Task EnsureDemoVehiclesAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.Vehicles.CountAsync(vehicle => vehicle.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var existingIds = new HashSet<string>(await db.Vehicles.Select(vehicle => vehicle.Id).ToListAsync(), StringComparer.OrdinalIgnoreCase);
    var regions = new[] { "Yangon", "Mandalay", "Nay Pyi Taw", "Bago", "Taunggyi" };
    var types = new[] { "Box Truck", "Delivery Van", "Reefer Truck", "Tanker" };
    var models = new[] { "Isuzu FVR", "Toyota HiAce", "Hino 500", "Mitsubishi Fuso", "Nissan Atlas" };
    var makes = new[] { "Isuzu", "Toyota", "Hino", "Mitsubishi", "Nissan" };
    var statuses = new[] { "Active", "Active", "Active", "Maintenance", "Inactive" };
    var drivers = new[] { "Driver User", "Aung Min" };
    var depots = new[] { "Yangon East Yard", "Mandalay Depot", "Central Workshop" };
    var fuelTypes = new[] { "Diesel", "Petrol", "Diesel", "CNG" };
    var added = 0;

    for (var index = 1; activeCount + added < DemoRecordTarget; index++)
    {
      var id = $"VH-{2000 + index:D4}";
      if (!existingIds.Add(id)) continue;

      var model = models[index % models.Length];
      var make = makes[index % makes.Length];
      var region = regions[index % regions.Length];
      var platePrefix = region == "Mandalay" ? "MDY" : region == "Nay Pyi Taw" ? "NPT" : region == "Bago" ? "BGO" : "YGN";
      db.Vehicles.Add(new Vehicle
      {
        Id = id,
        Plate = $"{platePrefix}-{5000 + index}",
        Region = region,
        Type = types[index % types.Length],
        Model = model,
        Make = make,
        Year = (2020 + index % 5).ToString(),
        Color = new[] { "White", "Silver", "Blue", "Red", "Gray" }[index % 5],
        Status = statuses[index % statuses.Length],
        Ownership = index % 3 == 0 ? "Leased" : "Owned",
        Driver = drivers[index % drivers.Length],
        DriverImage = string.Empty,
        Depot = depots[index % depots.Length],
        Capacity = $"{2 + index % 9} tons",
        FuelCapacity = $"{70 + index % 6 * 15} L",
        FuelType = fuelTypes[index % fuelTypes.Length],
        Vin = $"MMTDEMO{2020 + index % 5}{index:D5}",
        EngineNo = $"ENG-DEMO-{index:D4}",
        Odometer = (18000 + index * 1450).ToString(),
        LastService = $"2026-04-{(index % 20) + 1:D2}",
        NextService = $"2026-06-{(index % 20) + 1:D2}",
        ServiceNote = "Seeded demo vehicle for testing.",
        PurchaseCost = (38000 + index * 2200).ToString(),
        RegistrationNo = $"REG-{platePrefix}-{5000 + index}",
        RegistrationExpiry = $"2027-0{(index % 9) + 1}-28",
        RoadTaxExpiry = $"2027-0{(index % 9) + 1}-28",
        InsuranceExpiry = $"2027-0{(index % 9) + 1}-15",
        InsuranceProvider = index % 2 == 0 ? "Global Insurance" : "Apex Insurance",
        InsurancePolicy = $"POL-{2000 + index:D4}",
        InspectionDue = $"2026-0{(index % 4) + 6}-15",
        AcquiredDate = $"2024-0{(index % 9) + 1}-10",
        Image = string.Empty,
        IsDeleted = 0,
        CreatedAt = now.AddDays(-30 - index),
        UpdatedAt = now.AddDays(-(index % 10))
      });
      added++;
    }
  }

  private static async Task EnsureDemoTripsAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.Trips.CountAsync(trip => trip.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var vehicles = await db.Vehicles
      .Where(vehicle => vehicle.IsDeleted == 0)
      .Select(vehicle => new { vehicle.Id, vehicle.Plate, vehicle.Driver })
      .ToListAsync();
    var existingNumbers = new HashSet<string>(await db.Trips.Select(trip => trip.TripNumber).ToListAsync(), StringComparer.OrdinalIgnoreCase);
    var tripTypes = new[] { "Delivery", "Line Haul", "Pickup" };
    var statuses = new[] { "Pending", "In Transit", "Completed", "Completed", "Active" };
    var priorities = new[] { "Low", "Medium", "High", "Critical" };
    var cargoTypes = new[] { "General Cargo", "Cold Chain", "Hazardous" };
    var customers = new[] { "Yangon Retail Group", "Mandalay Wholesale", "Nay Pyi Taw Foods", "Bago Distribution", "Taunggyi Market" };
    var locations = new[] { "Yangon East Yard", "Mandalay Depot", "Main Office", "Central Workshop" };
    var added = 0;

    for (var index = 1; activeCount + added < DemoRecordTarget; index++)
    {
      var tripNumber = $"TRIP-{2000 + index:D4}";
      if (!existingNumbers.Add(tripNumber)) continue;

      var vehicle = vehicles.Count > 0 ? vehicles[index % vehicles.Count] : new { Id = "VH-1001", Plate = "YGN-1187", Driver = "Driver User" };
      db.Trips.Add(new Trip
      {
        TripNumber = tripNumber,
        TripType = tripTypes[index % tripTypes.Length],
        Status = statuses[index % statuses.Length],
        Priority = priorities[index % priorities.Length],
        CustomerName = customers[index % customers.Length],
        Department = "Operations",
        CostCenter = index % 2 == 0 ? "OPS-YGN" : "OPS-MDY",
        VehicleId = vehicle.Id,
        VehiclePlate = vehicle.Plate,
        TrailerNumber = index % 4 == 0 ? $"TRL-{index:D3}" : null,
        DriverName = string.IsNullOrWhiteSpace(vehicle.Driver) ? "Driver User" : vehicle.Driver,
        CoDriverName = index % 3 == 0 ? "Aung Min" : null,
        DispatcherName = "Dispatcher User",
        CargoType = cargoTypes[index % cargoTypes.Length],
        LoadWeightKg = 800 + index * 150,
        LoadVolumeM3 = 5 + index % 16,
        PickupLocation = locations[index % locations.Length],
        DropoffLocation = locations[(index + 1) % locations.Length],
        PickupContact = $"09-2200{index:D5}",
        DropoffContact = $"09-2300{index:D5}",
        DepartureDateTime = $"2026-05-{(index % 25) + 1:D2}T{(6 + index % 8):D2}:00",
        EstimatedArrival = $"2026-05-{(index % 25) + 1:D2}T{(12 + index % 8):D2}:30",
        ActualArrival = index % 5 is 2 or 3 ? $"2026-05-{(index % 25) + 1:D2}T{(12 + index % 8):D2}:10" : null,
        PlannedDistanceKm = 40 + index * 18,
        StartingOdometerKm = 18000 + index * 600,
        CurrentOdometerKm = 18040 + index * 610,
        EndingOdometerKm = index % 5 is 2 or 3 ? 18040 + index * 610 : null,
        FuelIssuedLiters = 20 + index % 12 * 5,
        TollEstimate = 5000 + index * 1200,
        PermitRequired = index % 4 == 0,
        TemperatureControlled = index % 6 == 0,
        SpecialInstructions = "Seeded trip for workflow testing.",
        DriverNotes = index % 2 == 0 ? "No issues reported." : null,
        IsDeleted = 0,
        CreatedAt = now.AddDays(-20 - index),
        UpdatedAt = now.AddDays(-(index % 8))
      });
      added++;
    }
  }

  private static async Task EnsureDemoMaintenanceTicketsAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.MaintenanceTickets.CountAsync(ticket => ticket.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var vehicles = await db.Vehicles.Where(vehicle => vehicle.IsDeleted == 0).Select(vehicle => new { vehicle.Id, vehicle.Model }).ToListAsync();
    var existingIds = new HashSet<string>(await db.MaintenanceTickets.Select(ticket => ticket.Id).ToListAsync(), StringComparer.OrdinalIgnoreCase);
    var issues = new[] { "Oil service", "Brake inspection", "Tire replacement", "Battery check", "Cooling system service" };
    var statuses = new[] { "Pending", "Active", "Completed", "Maintenance" };
    var mechanics = new[] { "Mechanic User", "Myo Zaw" };
    var added = 0;

    for (var index = 1; activeCount + added < DemoRecordTarget; index++)
    {
      var id = $"MT-{3000 + index:D4}";
      if (!existingIds.Add(id)) continue;

      var vehicle = vehicles.Count > 0 ? vehicles[index % vehicles.Count] : new { Id = "VH-1001", Model = "Isuzu FVR" };
      db.MaintenanceTickets.Add(new MaintenanceTicket
      {
        Id = id,
        Vehicle = vehicle.Model,
        VehicleId = vehicle.Id,
        Issue = issues[index % issues.Length],
        Details = "Seeded maintenance ticket for testing.",
        ReportedDate = $"2026-05-{(index % 25) + 1:D2}",
        Mechanic = mechanics[index % mechanics.Length],
        Status = statuses[index % statuses.Length],
        IsDeleted = 0,
        CreatedAt = now.AddDays(-18 - index),
        UpdatedAt = now.AddDays(-(index % 7))
      });
      added++;
    }
  }

  private static async Task EnsureDemoInventoryPartsAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.InventoryParts.CountAsync(part => part.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var existingIds = new HashSet<string>(await db.InventoryParts.Select(part => part.Id).ToListAsync(), StringComparer.OrdinalIgnoreCase);
    var names = new[] { "Air Filter", "Fuel Filter", "Brake Pad Set", "Engine Oil 15W-40", "Truck Tire 295/80R22.5", "Battery 12V", "Wiper Blade Set" };
    var categories = new[] { "Filters", "Brake", "Fluids", "Tires", "Electrical" };
    var suppliers = new[] { "Yangon Parts Co.", "Mandalay Auto Supply", "Fleet Tire Service" };
    var locations = new[] { "Central Workshop", "Yangon East Yard", "Mandalay Depot" };
    var added = 0;

    for (var index = 1; activeCount + added < DemoRecordTarget; index++)
    {
      var id = $"PART-{2000 + index:D4}";
      if (!existingIds.Add(id)) continue;

      db.InventoryParts.Add(new InventoryPart
      {
        Id = id,
        Name = names[index % names.Length],
        PartNo = $"SKU-{2000 + index:D4}",
        Category = categories[index % categories.Length],
        Stock = 3 + index % 45,
        ReorderPoint = 5 + index % 10,
        Supplier = suppliers[index % suppliers.Length],
        UnitCost = (18 + index * 3.75m).ToString("0.00"),
        Location = locations[index % locations.Length],
        Image = string.Empty,
        IsDeleted = 0,
        CreatedAt = now.AddDays(-25 - index),
        UpdatedAt = now.AddDays(-(index % 9))
      });
      added++;
    }
  }

  private static async Task EnsureDemoIncidentsAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.Incidents.CountAsync(incident => incident.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var vehicleIds = await db.Vehicles.Where(vehicle => vehicle.IsDeleted == 0).Select(vehicle => vehicle.Id).ToListAsync();
    var existingIds = new HashSet<string>(await db.Incidents.Select(incident => incident.Id).ToListAsync(), StringComparer.OrdinalIgnoreCase);
    var types = new[] { "Accident", "Breakdown", "Damage", "Theft", "Other" };
    var severities = new[] { "Low", "Medium", "High", "Critical" };
    var statuses = new[] { "Pending", "Active", "Completed" };
    var drivers = new[] { "Driver User", "Aung Min" };
    var added = 0;

    for (var index = 1; activeCount + added < DemoRecordTarget; index++)
    {
      var id = $"INC-{2000 + index:D4}";
      if (!existingIds.Add(id)) continue;

      db.Incidents.Add(new Incident
      {
        Id = id,
        VehicleId = vehicleIds.Count > 0 ? vehicleIds[index % vehicleIds.Count] : "VH-1001",
        Driver = drivers[index % drivers.Length],
        Date = $"2026-05-{(index % 25) + 1:D2}",
        Type = types[index % types.Length],
        Severity = severities[index % severities.Length],
        Status = statuses[index % statuses.Length],
        Cost = (index * 35.5m).ToString("0.00"),
        Notes = "Seeded incident for testing.",
        IsDeleted = 0,
        CreatedAt = now.AddDays(-12 - index),
        UpdatedAt = now.AddDays(-(index % 6))
      });
      added++;
    }
  }

  private static async Task EnsureDemoExpensesAsync(FleetDbContext db, DateTime now)
  {
    var activeCount = await db.Expenses.CountAsync(expense => expense.IsDeleted == 0);
    if (activeCount >= DemoRecordTarget) return;

    var vehicles = await db.Vehicles.Where(vehicle => vehicle.IsDeleted == 0).Select(vehicle => new { vehicle.Id, vehicle.Driver }).ToListAsync();
    var tripNumbers = await db.Trips.Where(trip => trip.IsDeleted == 0).Select(trip => trip.TripNumber).ToListAsync();
    var types = new[] { "Fuel", "Toll", "Repair", "Parking", "Insurance", "Tax" };
    var statuses = new[] { "Active", "Pending", "Completed" };

    for (var index = 1; activeCount < DemoRecordTarget; index++, activeCount++)
    {
      var vehicle = vehicles.Count > 0 ? vehicles[index % vehicles.Count] : new { Id = "VH-1001", Driver = "Driver User" };
      db.Expenses.Add(new Expense
      {
        ExpenseDate = $"2026-05-{(index % 25) + 1:D2}",
        ExpenseType = types[index % types.Length],
        VehicleId = vehicle.Id,
        TripNumber = tripNumbers.Count > 0 && index % 5 != 0 ? tripNumbers[index % tripNumbers.Count] : null,
        DriverName = string.IsNullOrWhiteSpace(vehicle.Driver) ? "Driver User" : vehicle.Driver,
        Amount = 25000 + index * 8500,
        Status = statuses[index % statuses.Length],
        Notes = "Seeded expense for testing.",
        IsDeleted = 0,
        CreatedAt = now.AddDays(-14 - index),
        UpdatedAt = now.AddDays(-(index % 8))
      });
    }
  }

  private static async Task EnsureDemoAuditLogsAsync(FleetDbContext db, DateTime now)
  {
    var count = await db.AuditLogs.CountAsync();
    if (count >= DemoRecordTarget) return;

    var roles = new[] { "admin", "dispatcher", "driver", "mechanic" };
    var modules = new[] { "vehicles", "trips", "maintenance-tickets", "inventory-parts", "incidents", "expenses" };
    var actions = new[] { "Create", "Edit", "Delete", "View" };

    for (var index = 1; count < DemoRecordTarget; index++, count++)
    {
      var module = modules[index % modules.Length];
      var action = actions[index % actions.Length];
      db.AuditLogs.Add(new AuditLog
      {
        RoleId = roles[index % roles.Length],
        ModuleKey = module,
        Action = action,
        EntityId = $"DEMO-{index:D4}",
        Description = $"{action} demo {module} record.",
        CreatedAt = now.AddMinutes(-index * 9)
      });
    }
  }

  private static async Task EnsureDemoStatusHistoryAsync(FleetDbContext db, DateTime now)
  {
    var count = await db.StatusHistories.CountAsync();
    if (count >= DemoRecordTarget) return;

    var entityTypes = new[] { "Vehicle", "Trip", "MaintenanceTicket", "Incident", "Expense" };
    var statuses = new[] { "Pending", "Active", "In Transit", "Completed", "Maintenance" };
    var roles = new[] { "admin", "dispatcher", "driver", "mechanic" };

    for (var index = 1; count < DemoRecordTarget; index++, count++)
    {
      db.StatusHistories.Add(new StatusHistory
      {
        EntityType = entityTypes[index % entityTypes.Length],
        EntityId = $"DEMO-{index:D4}",
        OldStatus = statuses[(index + statuses.Length - 1) % statuses.Length],
        NewStatus = statuses[index % statuses.Length],
        RoleId = roles[index % roles.Length],
        CreatedAt = now.AddMinutes(-index * 11)
      });
    }
  }

  private static async Task SeedTripSetupAsync<T>(
    DbSet<T> set,
    IEnumerable<(string Name, string Code, string Description)> values,
    DateTimeOffset now) where T : TripSetupCodeOption, new()
  {
    if (await set.AnyAsync()) return;

    set.AddRange(values.Select(value => new T
    {
      Name = value.Name,
      Code = value.Code,
      Description = value.Description,
      Status = "Active",
      CreatedAt = now,
      UpdatedAt = now
    }));
  }

  public static string ToSlug(string name, IEnumerable<string> existingIds)
  {
    var baseSlug = string.Concat(
      name
        .Trim()
        .ToLowerInvariant()
        .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'))
      .Split('-', StringSplitOptions.RemoveEmptyEntries)
      .DefaultIfEmpty("role")
      .Aggregate((left, right) => $"{left}-{right}");

    var taken = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
    if (!taken.Contains(baseSlug)) return baseSlug;

    var suffix = 2;
    while (taken.Contains($"{baseSlug}-{suffix}")) suffix++;
    return $"{baseSlug}-{suffix}";
  }

  public static string NextRoleCode(IEnumerable<string> existingCodes)
  {
    var max = existingCodes
      .Select(code =>
      {
        var parts = code.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && int.TryParse(parts[1], out var value) ? value : 0;
      })
      .DefaultIfEmpty(0)
      .Max();

    return $"ROL-{(max + 1):D4}";
  }
}
