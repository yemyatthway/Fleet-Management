namespace FleetManagement.Api.Data;

using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public static class SeedData
{
  public static readonly string[] FixedRoleIds = ["admin", "dispatcher", "driver", "mechanic"];

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

    await SeedSystemUsersAsync(db, now);
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
    var users = new List<Models.User>
    {
      BuildSeedUser("seed-admin", "Admin User", "admin@fleet.com", "admin", "System Administrator", "Administration", now),
      BuildSeedUser("seed-dispatcher", "Dispatcher User", "dispatcher@fleet.com", "dispatcher", "Dispatcher", "Operations", now),
      BuildSeedUser("seed-driver", "Driver User", "driver@fleet.com", "driver", "Driver", "Transport", now, "DRV-0001", "B", "2027-12-31"),
      BuildSeedUser("seed-mechanic", "Mechanic User", "mechanic@fleet.com", "mechanic", "Mechanic", "Maintenance", now)
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
      existingUser.PasswordHash = seedUser.PasswordHash;
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

  private static Models.User BuildSeedUser(
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
