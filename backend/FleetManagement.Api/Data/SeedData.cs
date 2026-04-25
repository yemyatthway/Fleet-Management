using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public static class SeedData
{
  public static async Task InitializeAsync(FleetDbContext db)
  {
    var now = DateTime.UtcNow;

    if (!await db.Roles.AnyAsync())
    {
      var roles = new List<Role>
      {
        new() { Id = "admin", Code = "ROL-0001", Name = "Admin", Description = "Full platform access with governance controls.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-14) },
        new() { Id = "dispatcher", Code = "ROL-0002", Name = "Dispatcher", Description = "Schedules routes, assigns drivers, and monitors trips.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-12) },
        new() { Id = "driver", Code = "ROL-0003", Name = "Driver", Description = "Executes assigned routes and updates trip status.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-10) },
        new() { Id = "mechanic", Code = "ROL-0004", Name = "Mechanic", Description = "Manages inspections, repairs, and maintenance logs.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-8), UpdatedAt = now.AddDays(-8) },
        new() { Id = "compliance", Code = "ROL-0005", Name = "Compliance", Description = "Audits fleet documents, permits, and safety records.", Status = "Active", IsDeleted = 0, CreatedAt = now.AddMonths(-6), UpdatedAt = now.AddDays(-6) }
      };

      db.Roles.AddRange(roles);
    }

    if (await db.Users.AnyAsync()) return;

    var users = new List<User>
    {
      new() { Id = "1", Name = "Sarah Johnson", EmployeeId = "EMP-1001", NrcNumber = "12/ZaYaTha/123456", Email = "sarah.johnson@fleet.com", RoleId = "admin", Status = "Active", Phone = "+1 (555) 123-4567", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Operations", Title = "Operations Manager", Location = "North Depot", Manager = "Evelyn Parker", LicenseNumber = "A1234567", LicenseClass = "C", LicenseExpiry = "2026-08-20", EmergencyContactName = "Mark Johnson", EmergencyContactRelation = "Spouse", EmergencyContactPhone = "+1 (555) 200-3001", Address = "120 Market St, Springfield, IL", TwoFactorEnabled = true, Notes = "Primary admin contact.", JoinDate = "2024-01-15", LastLogin = "2026-03-30T09:12:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-7), UpdatedAt = now.AddDays(-4) },
      new() { Id = "2", Name = "Michael Chen", EmployeeId = "EMP-1002", NrcNumber = "12/ZaYaTha/223456", Email = "michael.chen@fleet.com", RoleId = "dispatcher", Status = "Active", Phone = "+1 (555) 234-5678", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Dispatch", Title = "Lead Dispatcher", Location = "Central Hub", Manager = "Sarah Johnson", LicenseNumber = "B9087765", LicenseClass = "B", LicenseExpiry = "2025-11-02", EmergencyContactName = "Lily Chen", EmergencyContactRelation = "Sister", EmergencyContactPhone = "+1 (555) 200-3002", Address = "88 Pine Ave, Austin, TX", TwoFactorEnabled = true, Notes = "Oversees weekend coverage.", JoinDate = "2024-02-20", LastLogin = "2026-03-31T16:45:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-6), UpdatedAt = now.AddDays(-5) },
      new() { Id = "3", Name = "John Martinez", EmployeeId = "EMP-1003", NrcNumber = "12/ZaYaTha/323456", Email = "john.martinez@fleet.com", RoleId = "driver", Status = "Active", Phone = "+1 (555) 345-6789", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Fleet", Title = "Senior Driver", Location = "North Depot", Manager = "Riley Collins", LicenseNumber = "D4567289", LicenseClass = "A", LicenseExpiry = "2026-05-14", EmergencyContactName = "Maria Martinez", EmergencyContactRelation = "Spouse", EmergencyContactPhone = "+1 (555) 200-3003", Address = "45 Lake Rd, Chicago, IL", TwoFactorEnabled = false, Notes = "Assigned to long-haul routes.", JoinDate = "2023-11-10", LastLogin = "2026-04-01T06:32:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-5), UpdatedAt = now.AddDays(-3) },
      new() { Id = "4", Name = "Emily Davis", EmployeeId = "EMP-1004", NrcNumber = "12/ZaYaTha/423456", Email = "emily.davis@fleet.com", RoleId = "driver", Status = "Active", Phone = "+1 (555) 456-7890", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Fleet", Title = "Driver", Location = "East Depot", Manager = "Riley Collins", LicenseNumber = "D5567344", LicenseClass = "A", LicenseExpiry = "2025-09-18", EmergencyContactName = "Paul Davis", EmergencyContactRelation = "Father", EmergencyContactPhone = "+1 (555) 200-3004", Address = "560 River St, Boston, MA", TwoFactorEnabled = false, Notes = "Prefers early shifts.", JoinDate = "2024-03-05", LastLogin = "2026-04-01T08:05:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-5), UpdatedAt = now.AddDays(-2) },
      new() { Id = "5", Name = "Robert Wilson", EmployeeId = "EMP-1005", NrcNumber = "12/ZaYaTha/523456", Email = "robert.wilson@fleet.com", RoleId = "mechanic", Status = "Active", Phone = "+1 (555) 567-8901", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Maintenance", Title = "Lead Mechanic", Location = "Service Bay A", Manager = "Ken Morris", LicenseNumber = "M3344556", LicenseClass = "C", LicenseExpiry = "2027-01-10", EmergencyContactName = "Nina Wilson", EmergencyContactRelation = "Spouse", EmergencyContactPhone = "+1 (555) 200-3005", Address = "92 Elm St, Denver, CO", TwoFactorEnabled = true, Notes = "Specialized in diesel engines.", JoinDate = "2023-09-12", LastLogin = "2026-03-29T14:22:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-4), UpdatedAt = now.AddDays(-1) },
      new() { Id = "6", Name = "Jessica Brown", EmployeeId = "EMP-1006", NrcNumber = "12/ZaYaTha/623456", Email = "jessica.brown@fleet.com", RoleId = "driver", Status = "Active", Phone = "+1 (555) 678-9012", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Fleet", Title = "Driver", Location = "South Depot", Manager = "Riley Collins", LicenseNumber = "D7788990", LicenseClass = "A", LicenseExpiry = "2026-03-12", EmergencyContactName = "Alex Brown", EmergencyContactRelation = "Brother", EmergencyContactPhone = "+1 (555) 200-3006", Address = "18 Cedar Blvd, Phoenix, AZ", TwoFactorEnabled = false, Notes = "Night shift coverage.", JoinDate = "2024-01-28", LastLogin = "2026-04-02T05:50:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-4), UpdatedAt = now.AddDays(-2) },
      new() { Id = "7", Name = "David Lee", EmployeeId = "EMP-1007", NrcNumber = "12/ZaYaTha/723456", Email = "david.lee@fleet.com", RoleId = "dispatcher", Status = "Active", Phone = "+1 (555) 789-0123", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Dispatch", Title = "Dispatcher", Location = "Central Hub", Manager = "Sarah Johnson", LicenseNumber = "B5566123", LicenseClass = "B", LicenseExpiry = "2025-12-18", EmergencyContactName = "Grace Lee", EmergencyContactRelation = "Mother", EmergencyContactPhone = "+1 (555) 200-3007", Address = "301 Oak Dr, Seattle, WA", TwoFactorEnabled = true, Notes = "Handles urgent reroutes.", JoinDate = "2023-12-05", LastLogin = "2026-03-30T19:10:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-6), UpdatedAt = now.AddDays(-5) },
      new() { Id = "8", Name = "Amanda Taylor", EmployeeId = "EMP-1008", NrcNumber = "12/ZaYaTha/823456", Email = "amanda.taylor@fleet.com", RoleId = "mechanic", Status = "Active", Phone = "+1 (555) 890-1234", Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Maintenance", Title = "Mechanic", Location = "Service Bay B", Manager = "Ken Morris", LicenseNumber = "M7788332", LicenseClass = "C", LicenseExpiry = "2026-06-09", EmergencyContactName = "Oliver Taylor", EmergencyContactRelation = "Spouse", EmergencyContactPhone = "+1 (555) 200-3008", Address = "440 Maple St, Portland, OR", TwoFactorEnabled = false, Notes = "Tracks parts inventory.", JoinDate = "2024-02-14", LastLogin = "2026-03-28T12:40:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-4), UpdatedAt = now.AddDays(-1) },
      new() { Id = "9", Name = "Olivia Clark", EmployeeId = "EMP-1009", NrcNumber = "12/ZaYaTha/923456", Email = "olivia.clark@fleet.com", RoleId = "compliance", Status = "Active", Phone = "+1 (555) 222-1188", Avatar = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=facearea&w=160&h=160&q=80", NrcFront = "", NrcBack = "", Department = "Operations", Title = "Compliance Analyst", Location = "Central Hub", Manager = "Sarah Johnson", LicenseNumber = null, LicenseClass = null, LicenseExpiry = null, EmergencyContactName = "Noah Clark", EmergencyContactRelation = "Brother", EmergencyContactPhone = "+1 (555) 211-0088", Address = "77 State St, Sacramento, CA", TwoFactorEnabled = true, Notes = "Reviews permits and insurance packs.", JoinDate = "2024-04-08", LastLogin = "2026-04-04T09:10:00Z", IsDeleted = 0, CreatedAt = now.AddMonths(-3), UpdatedAt = now }
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
