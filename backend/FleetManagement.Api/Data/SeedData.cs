using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public static class SeedData
{
  public static readonly string[] FixedRoleIds = ["admin", "dispatcher", "driver", "mechanic"];

  public static async Task InitializeAsync(FleetDbContext db)
  {
    var now = DateTime.UtcNow;
    var fixedRoles = new List<Role>
    {
      new() { Id = "admin", Code = "ROL-0001", Name = "Admin", Description = "Full platform access with governance controls.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-14) },
      new() { Id = "dispatcher", Code = "ROL-0002", Name = "Dispatcher", Description = "Schedules routes, assigns drivers, and monitors trips.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-12) },
      new() { Id = "driver", Code = "ROL-0003", Name = "Driver", Description = "Executes assigned routes and updates trip status.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-10) },
      new() { Id = "mechanic", Code = "ROL-0004", Name = "Mechanic", Description = "Manages inspections, repairs, and maintenance logs.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-8) }
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

    var fixedDepartments = new List<DepartmentCodeOption>
    {
      new() { Name = "Operations", Description = "Operations planning and daily oversight.", Status = "Active", CreatedAt = DateTimeOffset.UtcNow.AddDays(-12), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-4) },
      new() { Name = "Dispatch", Description = "Route assignment and dispatch coordination.", Status = "Active", CreatedAt = DateTimeOffset.UtcNow.AddDays(-11), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3) },
      new() { Name = "Fleet", Description = "Fleet execution, drivers, and trip management.", Status = "Active", CreatedAt = DateTimeOffset.UtcNow.AddDays(-10), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
      new() { Name = "Maintenance", Description = "Vehicle maintenance, inspections, and repairs.", Status = "Active", CreatedAt = DateTimeOffset.UtcNow.AddDays(-9), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
    };

    var existingDepartments = await db.DepartmentCodeOptions.ToListAsync();
    foreach (var fixedDepartment in fixedDepartments)
    {
      var existingDepartment = existingDepartments.FirstOrDefault(department =>
        string.Equals(department.Name, fixedDepartment.Name, StringComparison.OrdinalIgnoreCase));

      if (existingDepartment is null)
      {
        db.DepartmentCodeOptions.Add(fixedDepartment);
        continue;
      }

      existingDepartment.Description = fixedDepartment.Description;
      existingDepartment.Status = "Active";
      existingDepartment.UpdatedAt = DateTimeOffset.UtcNow;
    }

    if (!await db.LocationCodeOptions.AnyAsync())
    {
      var locationNow = DateTimeOffset.UtcNow;
      var locations = new List<LocationCodeOption>
      {
        new()
        {
          Name = "Bago Main Warehouse",
          Code = "BG-WH-01",
          Type = "Warehouse",
          Address = "No. 23, Main Road, Bago",
          City = "Bago",
          Country = "Myanmar",
          ContactPerson = "Ko Aung",
          Phone = "09-123456789",
          OperatingHours = "08:00 - 18:00",
          Status = "Active",
          Notes = "Near highway, easy truck access",
          CreatedAt = locationNow.AddDays(-12)
        },
        new()
        {
          Name = "Yangon Dispatch Hub",
          Code = "YG-HB-01",
          Type = "Hub",
          Address = "No. 11, Industrial Zone Road, Yangon",
          City = "Yangon",
          Country = "Myanmar",
          ContactPerson = "Daw Mya",
          Phone = "09-555123456",
          OperatingHours = "24/7",
          Status = "Active",
          Notes = "Main routing and dispatch control center",
          CreatedAt = locationNow.AddDays(-10)
        },
        new()
        {
          Name = "Mandalay Service Yard",
          Code = "MD-SY-01",
          Type = "Service Yard",
          Address = "No. 88, Pyigyitagon Road, Mandalay",
          City = "Mandalay",
          Country = "Myanmar",
          ContactPerson = "U Min Latt",
          Phone = "09-777888999",
          OperatingHours = "07:00 - 19:00",
          Status = "Active",
          Notes = "Heavy vehicle service and overnight parking",
          CreatedAt = locationNow.AddDays(-8)
        }
      };

      db.LocationCodeOptions.AddRange(locations);
    }

    if (!await db.LocationTypeCodeOptions.AnyAsync())
    {
      var locationTypeNow = DateTimeOffset.UtcNow;
      var locationTypes = new List<LocationTypeCodeOption>
      {
        new() { Name = "Warehouse", Code = "LT-WH", Description = "Storage location for inventory, cargo, and staging.", Status = "Active", CreatedAt = locationTypeNow.AddDays(-12), UpdatedAt = locationTypeNow.AddDays(-4) },
        new() { Name = "Depot", Code = "LT-DP", Description = "Fleet depot for dispatch, parking, and route support.", Status = "Active", CreatedAt = locationTypeNow.AddDays(-11), UpdatedAt = locationTypeNow.AddDays(-3) },
        new() { Name = "Hub", Code = "LT-HB", Description = "Central transfer point for routing and consolidation.", Status = "Active", CreatedAt = locationTypeNow.AddDays(-10), UpdatedAt = locationTypeNow.AddDays(-2) },
        new() { Name = "Yard", Code = "LT-YD", Description = "Outdoor staging or service yard for fleet operations.", Status = "Active", CreatedAt = locationTypeNow.AddDays(-9), UpdatedAt = locationTypeNow.AddDays(-1) }
      };

      db.LocationTypeCodeOptions.AddRange(locationTypes);
    }

    if (!await db.VehicleTypeCodeOptions.AnyAsync())
    {
      var vehicleTypeNow = DateTimeOffset.UtcNow;
      var vehicleTypes = new List<VehicleTypeCodeOption>
      {
        new() { Name = "Box Truck", Code = "VT-BOX", Description = "Enclosed cargo vehicle for general delivery routes.", Status = "Active", CreatedAt = vehicleTypeNow.AddDays(-12), UpdatedAt = vehicleTypeNow.AddDays(-4) },
        new() { Name = "Cargo Van", Code = "VT-VAN", Description = "Light-duty van for city deliveries and small loads.", Status = "Active", CreatedAt = vehicleTypeNow.AddDays(-11), UpdatedAt = vehicleTypeNow.AddDays(-3) },
        new() { Name = "Reefer Truck", Code = "VT-REEFER", Description = "Temperature-controlled vehicle for cold-chain shipments.", Status = "Active", CreatedAt = vehicleTypeNow.AddDays(-10), UpdatedAt = vehicleTypeNow.AddDays(-2) },
        new() { Name = "Flatbed", Code = "VT-FLAT", Description = "Open-bed truck for oversized or palletized freight.", Status = "Active", CreatedAt = vehicleTypeNow.AddDays(-9), UpdatedAt = vehicleTypeNow.AddDays(-1) }
      };

      db.VehicleTypeCodeOptions.AddRange(vehicleTypes);
    }

    if (!await db.FuelTypeCodeOptions.AnyAsync())
    {
      var fuelTypeNow = DateTimeOffset.UtcNow;
      var fuelTypes = new List<FuelTypeCodeOption>
      {
        new() { Name = "Diesel", Code = "FT-DIESEL", Description = "Standard diesel fuel for commercial vehicles.", Status = "Active", CreatedAt = fuelTypeNow.AddDays(-12), UpdatedAt = fuelTypeNow.AddDays(-4) },
        new() { Name = "Gasoline", Code = "FT-GAS", Description = "Gasoline fuel for light-duty vehicles.", Status = "Active", CreatedAt = fuelTypeNow.AddDays(-11), UpdatedAt = fuelTypeNow.AddDays(-3) },
        new() { Name = "Electric", Code = "FT-EV", Description = "Battery electric vehicle power source.", Status = "Active", CreatedAt = fuelTypeNow.AddDays(-10), UpdatedAt = fuelTypeNow.AddDays(-2) },
        new() { Name = "Hybrid", Code = "FT-HYBRID", Description = "Hybrid fuel and electric powertrain.", Status = "Active", CreatedAt = fuelTypeNow.AddDays(-9), UpdatedAt = fuelTypeNow.AddDays(-1) }
      };

      db.FuelTypeCodeOptions.AddRange(fuelTypes);
    }

    if (!await db.MaintenanceTickets.AnyAsync())
    {
      var tickets = new List<MaintenanceTicket>
      {
        new() { Id = "MT-2031", Vehicle = "Box Truck", VehicleId = "VH-2048", Issue = "Brake Inspection", Details = "Scheduled brake pad replacement", ReportedDate = "2026-02-28", Mechanic = "Daniel Harris", Status = "Pending", IsDeleted = 0, CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-20) },
        new() { Id = "MT-2032", Vehicle = "Cargo Van", VehicleId = "VH-3054", Issue = "Engine Overheat", Details = "Cooling system diagnostics", ReportedDate = "2026-03-02", Mechanic = "Maya Lopez", Status = "Repairing", IsDeleted = 0, CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-16) },
        new() { Id = "MT-2033", Vehicle = "Reefer Truck", VehicleId = "VH-1987", Issue = "Refrigeration Unit", Details = "Temperature fluctuation detected", ReportedDate = "2026-02-22", Mechanic = "Alex Chen", Status = "Completed", IsDeleted = 0, CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-12) },
        new() { Id = "MT-2034", Vehicle = "Flatbed", VehicleId = "VH-4129", Issue = "Hydraulic Leak", Details = "Seal replacement required", ReportedDate = "2026-03-05", Mechanic = "Isabella Park", Status = "Repairing", IsDeleted = 0, CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-8) },
        new() { Id = "MT-2035", Vehicle = "Delivery Van", VehicleId = "VH-2661", Issue = "Tire Alignment", Details = "Front axle alignment", ReportedDate = "2026-03-01", Mechanic = "Marcus Reed", Status = "Pending", IsDeleted = 0, CreatedAt = now.AddDays(-17), UpdatedAt = now.AddDays(-17) }
      };

      db.MaintenanceTickets.AddRange(tickets);
    }

    if (await db.Users.AnyAsync())
    {
      await db.SaveChangesAsync();
      return;
    }

    var users = new List<User>
    {
      new() { Id = "1", Name = "Sarah Johnson", EmployeeId = "EMP-1001", NrcNumber = "12/ZaYaTha/123456", Email = "sarah.johnson@fleet.com", RoleId = "admin", Status = "Active", Phone = "+1 (555) 123-4567", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Operations", Title = "Operations Manager", Location = "Bago Main Warehouse", Manager = "Evelyn Parker", LicenseNumber = "A1234567", LicenseClass = "C", LicenseExpiry = "2026-08-20", EmergencyContactName = "Mark Johnson", EmergencyContactRelation = "Spouse", EmergencyContactPhone = "+1 (555) 200-3001", Address = "120 Market St, Springfield, IL", TwoFactorEnabled = true, Notes = "Primary admin contact.", JoinDate = "2024-01-15", LastLogin = "2026-03-30T09:12:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-7), UpdatedAt = now.AddDays(-4) },
      new() { Id = "2", Name = "Michael Chen", EmployeeId = "EMP-1002", NrcNumber = "12/ZaYaTha/223456", Email = "michael.chen@fleet.com", RoleId = "dispatcher", Status = "Active", Phone = "+1 (555) 234-5678", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Dispatch", Title = "Lead Dispatcher", Location = "Yangon Dispatch Hub", Manager = "Sarah Johnson", LicenseNumber = "B9087765", LicenseClass = "B", LicenseExpiry = "2025-11-02", EmergencyContactName = "Lily Chen", EmergencyContactRelation = "Sister", EmergencyContactPhone = "+1 (555) 200-3002", Address = "88 Pine Ave, Austin, TX", TwoFactorEnabled = true, Notes = "Oversees weekend coverage.", JoinDate = "2024-02-20", LastLogin = "2026-03-31T16:45:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-6), UpdatedAt = now.AddDays(-5) },
      new() { Id = "3", Name = "John Martinez", EmployeeId = "EMP-1003", NrcNumber = "12/ZaYaTha/323456", Email = "john.martinez@fleet.com", RoleId = "driver", Status = "Active", Phone = "+1 (555) 345-6789", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Fleet", Title = "Senior Driver", Location = "Bago Main Warehouse", Manager = "Riley Collins", LicenseNumber = "D4567289", LicenseClass = "A", LicenseExpiry = "2026-05-14", EmergencyContactName = "Maria Martinez", EmergencyContactRelation = "Spouse", EmergencyContactPhone = "+1 (555) 200-3003", Address = "45 Lake Rd, Chicago, IL", TwoFactorEnabled = false, Notes = "Assigned to long-haul routes.", JoinDate = "2023-11-10", LastLogin = "2026-04-01T06:32:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-5), UpdatedAt = now.AddDays(-3) }
    };

    db.Users.AddRange(users);
    await db.SaveChangesAsync();
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
