using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(FleetDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var roles = new[]
        {
            new Role
            {
                Name = "Admin",
                Description = "Full access to manage users, reports, roles, and system settings.",
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Role
            {
                Name = "Dispatcher",
                Description = "Assign routes, monitor trips, and coordinate drivers.",
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Role
            {
                Name = "Driver",
                Description = "View schedules, update trip status, and log issues.",
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Role
            {
                Name = "Mechanic",
                Description = "Manage maintenance tickets, inspections, and repair logs.",
                Status = "Disabled",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        db.Roles.AddRange(roles);
        await db.SaveChangesAsync();

        const string documentPlaceholder =
            "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==";

        db.Users.AddRange(
            new User
            {
                Name = "Alex Morgan",
                EmployeeId = "EMP-1001",
                NrcNumber = "12/ZaYaTha/123456",
                Email = "alex.morgan@fleet.local",
                Phone = "+1 (555) 010-1001",
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-180)),
                LastLogin = DateTimeOffset.UtcNow.AddDays(-1),
                Avatar = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80",
                NrcFront = documentPlaceholder,
                NrcBack = documentPlaceholder,
                Department = "Operations",
                Title = "Operations Manager",
                Location = "HQ",
                Manager = "Evelyn Parker",
                LicenseNumber = "A1234567",
                LicenseClass = "C",
                LicenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                EmergencyContactName = "Jordan Morgan",
                EmergencyContactRelation = "Spouse",
                EmergencyContactPhone = "+1 (555) 200-3001",
                Address = "120 Market St, Springfield, IL",
                TwoFactorEnabled = true,
                Notes = "Primary admin contact.",
                RoleId = roles[0].Id
            },
            new User
            {
                Name = "Maya Chen",
                EmployeeId = "EMP-1002",
                NrcNumber = "12/ZaYaTha/223456",
                Email = "maya.chen@fleet.local",
                Phone = "+1 (555) 010-1002",
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)),
                LastLogin = DateTimeOffset.UtcNow.AddHours(-12),
                Avatar = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=facearea&w=160&h=160&q=80",
                NrcFront = documentPlaceholder,
                NrcBack = documentPlaceholder,
                Department = "Dispatch",
                Title = "Lead Dispatcher",
                Location = "Central Hub",
                Manager = "Alex Morgan",
                LicenseNumber = "B9087765",
                LicenseClass = "B",
                LicenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10)),
                EmergencyContactName = "Lily Chen",
                EmergencyContactRelation = "Sister",
                EmergencyContactPhone = "+1 (555) 200-3002",
                Address = "88 Pine Ave, Austin, TX",
                TwoFactorEnabled = true,
                Notes = "Oversees weekend coverage.",
                RoleId = roles[1].Id
            },
            new User
            {
                Name = "Chris Taylor",
                EmployeeId = "EMP-1003",
                NrcNumber = "12/ZaYaTha/323456",
                Email = "chris.taylor@fleet.local",
                Phone = "+1 (555) 010-1003",
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-45)),
                LastLogin = DateTimeOffset.UtcNow.AddHours(-4),
                Avatar = "https://images.unsplash.com/photo-1527980965255-d3b416303d12?auto=format&fit=facearea&w=160&h=160&q=80",
                NrcFront = documentPlaceholder,
                NrcBack = documentPlaceholder,
                Department = "Fleet",
                Title = "Driver",
                Location = "North Depot",
                Manager = "Maya Chen",
                LicenseNumber = "D4567289",
                LicenseClass = "A",
                LicenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                EmergencyContactName = "Riley Taylor",
                EmergencyContactRelation = "Sibling",
                EmergencyContactPhone = "+1 (555) 200-3003",
                Address = "45 Lake Rd, Chicago, IL",
                TwoFactorEnabled = false,
                Notes = "Assigned to regional routes.",
                RoleId = roles[2].Id
            });

        await db.SaveChangesAsync();
    }
}
