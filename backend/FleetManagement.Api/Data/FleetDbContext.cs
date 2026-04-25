using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
  public DbSet<Role> Roles => Set<Role>();
  public DbSet<User> Users => Set<User>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Role>(entity =>
    {
      entity.HasKey(r => r.Id);
      entity.Property(r => r.Id).HasMaxLength(80);
      entity.Property(r => r.Code).HasMaxLength(20);
      entity.Property(r => r.Name).HasMaxLength(120);
      entity.Property(r => r.Description).HasMaxLength(500);
      entity.Property(r => r.Status).HasMaxLength(30);
      entity.HasIndex(r => r.Code).IsUnique();
    });

    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(u => u.Id);
      entity.Property(u => u.Id).HasMaxLength(80);
      entity.Property(u => u.Name).HasMaxLength(120);
      entity.Property(u => u.EmployeeId).HasMaxLength(80);
      entity.Property(u => u.NrcNumber).HasMaxLength(80);
      entity.Property(u => u.Email).HasMaxLength(160);
      entity.Property(u => u.RoleId).HasMaxLength(80);
      entity.Property(u => u.Status).HasMaxLength(30);
      entity.Property(u => u.Phone).HasMaxLength(40);
      entity.Property(u => u.Avatar).HasMaxLength(500);
      entity.Property(u => u.NrcFront).HasMaxLength(500);
      entity.Property(u => u.NrcBack).HasMaxLength(500);
      entity.Property(u => u.Department).HasMaxLength(120);
      entity.Property(u => u.Title).HasMaxLength(120);
      entity.Property(u => u.Location).HasMaxLength(120);
      entity.Property(u => u.Manager).HasMaxLength(120);
      entity.Property(u => u.LicenseNumber).HasMaxLength(80);
      entity.Property(u => u.LicenseClass).HasMaxLength(40);
      entity.Property(u => u.LicenseExpiry).HasMaxLength(40);
      entity.Property(u => u.EmergencyContactName).HasMaxLength(120);
      entity.Property(u => u.EmergencyContactRelation).HasMaxLength(80);
      entity.Property(u => u.EmergencyContactPhone).HasMaxLength(40);
      entity.Property(u => u.Address).HasMaxLength(255);
      entity.Property(u => u.Notes).HasMaxLength(2000);
      entity.Property(u => u.JoinDate).HasMaxLength(40);
      entity.Property(u => u.LastLogin).HasMaxLength(80);
      entity.HasIndex(u => u.EmployeeId);
      entity.HasIndex(u => u.Email);

      entity
        .HasOne(u => u.Role)
        .WithMany(r => r.Users)
        .HasForeignKey(u => u.RoleId)
        .OnDelete(DeleteBehavior.Cascade);
    });
  }
}
