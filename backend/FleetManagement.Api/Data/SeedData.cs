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

        db.Users.AddRange(
            new User
            {
                Name = "Alex Morgan",
                Email = "alex.morgan@fleet.local",
                Phone = "+1 (555) 010-1001",
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-180)),
                RoleId = roles[0].Id
            },
            new User
            {
                Name = "Maya Chen",
                Email = "maya.chen@fleet.local",
                Phone = "+1 (555) 010-1002",
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)),
                RoleId = roles[1].Id
            },
            new User
            {
                Name = "Chris Taylor",
                Email = "chris.taylor@fleet.local",
                Phone = "+1 (555) 010-1003",
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-45)),
                RoleId = roles[2].Id
            });

        await db.SaveChangesAsync();
    }
}
