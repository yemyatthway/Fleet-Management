using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(role => role.Name).IsUnique();

            entity.Property(role => role.Name).HasMaxLength(80).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(300).IsRequired();
            entity.Property(role => role.Status).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.EmployeeId).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.NrcNumber).IsUnique();

            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.EmployeeId).HasMaxLength(40).IsRequired();
            entity.Property(user => user.NrcNumber).HasMaxLength(80).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Phone).HasMaxLength(40).IsRequired();
            entity.Property(user => user.Status).HasMaxLength(20).IsRequired();
            entity.Property(user => user.Department).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Title).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Location).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Manager).HasMaxLength(120).IsRequired();
            entity.Property(user => user.LicenseNumber).HasMaxLength(80);
            entity.Property(user => user.LicenseClass).HasMaxLength(40);
            entity.Property(user => user.EmergencyContactName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.EmergencyContactRelation).HasMaxLength(80).IsRequired();
            entity.Property(user => user.EmergencyContactPhone).HasMaxLength(40).IsRequired();
            entity.Property(user => user.Address).HasMaxLength(300).IsRequired();
            entity.Property(user => user.Notes).HasMaxLength(1000);

            entity
                .HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
