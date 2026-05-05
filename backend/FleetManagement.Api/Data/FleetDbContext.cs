using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
  public DbSet<Role> Roles => Set<Role>();
  public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
  public DbSet<User> Users => Set<User>();
  public DbSet<DepartmentCodeOption> DepartmentCodeOptions => Set<DepartmentCodeOption>();
  public DbSet<LocationCodeOption> LocationCodeOptions => Set<LocationCodeOption>();
  public DbSet<LocationTypeCodeOption> LocationTypeCodeOptions => Set<LocationTypeCodeOption>();
  public DbSet<VehicleTypeCodeOption> VehicleTypeCodeOptions => Set<VehicleTypeCodeOption>();
  public DbSet<FuelTypeCodeOption> FuelTypeCodeOptions => Set<FuelTypeCodeOption>();
  public DbSet<Vehicle> Vehicles => Set<Vehicle>();
  public DbSet<Trip> Trips => Set<Trip>();
  public DbSet<TripTypeCodeOption> TripTypeCodeOptions => Set<TripTypeCodeOption>();
  public DbSet<CargoTypeCodeOption> CargoTypeCodeOptions => Set<CargoTypeCodeOption>();
  public DbSet<StatusCodeOption> StatusCodeOptions => Set<StatusCodeOption>();
  public DbSet<TripPriorityCodeOption> TripPriorityCodeOptions => Set<TripPriorityCodeOption>();
  public DbSet<IncidentTypeCodeOption> IncidentTypeCodeOptions => Set<IncidentTypeCodeOption>();
  public DbSet<SeverityCodeOption> SeverityCodeOptions => Set<SeverityCodeOption>();
  public DbSet<ExpenseTypeCodeOption> ExpenseTypeCodeOptions => Set<ExpenseTypeCodeOption>();
  public DbSet<MaintenanceTypeCodeOption> MaintenanceTypeCodeOptions => Set<MaintenanceTypeCodeOption>();
  public DbSet<DocumentTypeCodeOption> DocumentTypeCodeOptions => Set<DocumentTypeCodeOption>();
  public DbSet<SupplierCodeOption> SupplierCodeOptions => Set<SupplierCodeOption>();
  public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
  public DbSet<InventoryPart> InventoryParts => Set<InventoryPart>();
  public DbSet<Incident> Incidents => Set<Incident>();
  public DbSet<Expense> Expenses => Set<Expense>();
  public DbSet<FleetDocument> FleetDocuments => Set<FleetDocument>();
  public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
  public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();

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

    modelBuilder.Entity<RolePermission>(entity =>
    {
      entity.ToTable("RolePermissions");
      entity.HasKey(permission => permission.Id);
      entity.Property(permission => permission.RoleId).HasMaxLength(80).IsRequired();
      entity.Property(permission => permission.ModuleKey).HasMaxLength(80).IsRequired();
      entity.HasIndex(permission => new { permission.RoleId, permission.ModuleKey }).IsUnique();

      entity
        .HasOne(permission => permission.Role)
        .WithMany()
        .HasForeignKey(permission => permission.RoleId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(u => u.Id);
      entity.Property(u => u.Id).HasMaxLength(80);
      entity.Property(u => u.Name).HasMaxLength(120);
      entity.Property(u => u.EmployeeId).HasMaxLength(80);
      entity.Property(u => u.NrcNumber).HasMaxLength(80);
      entity.Property(u => u.Email).HasMaxLength(160);
      entity.Property(u => u.PasswordHash).HasMaxLength(128);
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

    modelBuilder.Entity<LocationCodeOption>(entity =>
    {
      entity.ToTable("LocationCodeOptions");
      entity.HasKey(location => location.Id);
      entity.HasIndex(location => location.Name).IsUnique();
      entity.HasIndex(location => location.Code).IsUnique();
      entity.Property(location => location.Name).HasMaxLength(120).IsRequired();
      entity.Property(location => location.Code).HasMaxLength(40).IsRequired();
      entity.Property(location => location.Type).HasMaxLength(50).IsRequired();
      entity.Property(location => location.Address).HasMaxLength(300).IsRequired();
      entity.Property(location => location.City).HasMaxLength(120).IsRequired();
      entity.Property(location => location.Country).HasMaxLength(120).IsRequired();
      entity.Property(location => location.ContactPerson).HasMaxLength(120);
      entity.Property(location => location.Phone).HasMaxLength(40).IsRequired();
      entity.Property(location => location.OperatingHours).HasMaxLength(80).IsRequired();
      entity.Property(location => location.Notes).HasMaxLength(500);
      entity.Property(location => location.Status).HasMaxLength(20).IsRequired();
    });

    modelBuilder.Entity<DepartmentCodeOption>(entity =>
    {
      entity.ToTable("DepartmentCodeOptions");
      entity.HasKey(department => department.Id);
      entity.HasIndex(department => department.Name).IsUnique();
      entity.Property(department => department.Name).HasMaxLength(120).IsRequired();
      entity.Property(department => department.Description).HasMaxLength(500);
      entity.Property(department => department.Status).HasMaxLength(20).IsRequired();
    });

    modelBuilder.Entity<LocationTypeCodeOption>(entity =>
    {
      entity.ToTable("LocationTypeCodeOptions");
      entity.HasKey(locationType => locationType.Id);
      entity.HasIndex(locationType => locationType.Name).IsUnique();
      entity.HasIndex(locationType => locationType.Code).IsUnique();
      entity.Property(locationType => locationType.Name).HasMaxLength(120).IsRequired();
      entity.Property(locationType => locationType.Code).HasMaxLength(40).IsRequired();
      entity.Property(locationType => locationType.Description).HasMaxLength(500);
      entity.Property(locationType => locationType.Status).HasMaxLength(20).IsRequired();
    });

    modelBuilder.Entity<VehicleTypeCodeOption>(entity =>
    {
      entity.ToTable("VehicleTypeCodeOptions");
      entity.HasKey(vehicleType => vehicleType.Id);
      entity.HasIndex(vehicleType => vehicleType.Name).IsUnique();
      entity.HasIndex(vehicleType => vehicleType.Code).IsUnique();
      entity.Property(vehicleType => vehicleType.Name).HasMaxLength(120).IsRequired();
      entity.Property(vehicleType => vehicleType.Code).HasMaxLength(40).IsRequired();
      entity.Property(vehicleType => vehicleType.Description).HasMaxLength(500);
      entity.Property(vehicleType => vehicleType.Status).HasMaxLength(20).IsRequired();
    });

    modelBuilder.Entity<FuelTypeCodeOption>(entity =>
    {
      entity.ToTable("FuelTypeCodeOptions");
      entity.HasKey(fuelType => fuelType.Id);
      entity.HasIndex(fuelType => fuelType.Name).IsUnique();
      entity.HasIndex(fuelType => fuelType.Code).IsUnique();
      entity.Property(fuelType => fuelType.Name).HasMaxLength(120).IsRequired();
      entity.Property(fuelType => fuelType.Code).HasMaxLength(40).IsRequired();
      entity.Property(fuelType => fuelType.Description).HasMaxLength(500);
      entity.Property(fuelType => fuelType.Status).HasMaxLength(20).IsRequired();
    });

    modelBuilder.Entity<Vehicle>(entity =>
    {
      entity.ToTable("Vehicles");
      entity.HasKey(vehicle => vehicle.Id);
      entity.HasIndex(vehicle => vehicle.Plate).IsUnique();
      entity.Property(vehicle => vehicle.Id).HasMaxLength(40);
      entity.Property(vehicle => vehicle.Plate).HasMaxLength(40).IsRequired();
      entity.Property(vehicle => vehicle.Region).HasMaxLength(120).IsRequired();
      entity.Property(vehicle => vehicle.Type).HasMaxLength(120).IsRequired();
      entity.Property(vehicle => vehicle.Model).HasMaxLength(120).IsRequired();
      entity.Property(vehicle => vehicle.Make).HasMaxLength(120);
      entity.Property(vehicle => vehicle.Year).HasMaxLength(20);
      entity.Property(vehicle => vehicle.Color).HasMaxLength(80);
      entity.Property(vehicle => vehicle.Status).HasMaxLength(30).IsRequired();
      entity.Property(vehicle => vehicle.Ownership).HasMaxLength(40);
      entity.Property(vehicle => vehicle.Driver).HasMaxLength(120).IsRequired();
      entity.Property(vehicle => vehicle.DriverImage).HasMaxLength(500);
      entity.Property(vehicle => vehicle.Depot).HasMaxLength(120);
      entity.Property(vehicle => vehicle.Capacity).HasMaxLength(80);
      entity.Property(vehicle => vehicle.FuelCapacity).HasMaxLength(80);
      entity.Property(vehicle => vehicle.FuelType).HasMaxLength(80).IsRequired();
      entity.Property(vehicle => vehicle.Vin).HasMaxLength(120);
      entity.Property(vehicle => vehicle.EngineNo).HasMaxLength(120);
      entity.Property(vehicle => vehicle.Odometer).HasMaxLength(80);
      entity.Property(vehicle => vehicle.LastService).HasMaxLength(40);
      entity.Property(vehicle => vehicle.NextService).HasMaxLength(40);
      entity.Property(vehicle => vehicle.ServiceNote).HasMaxLength(255);
      entity.Property(vehicle => vehicle.PurchaseCost).HasMaxLength(80);
      entity.Property(vehicle => vehicle.RegistrationNo).HasMaxLength(120);
      entity.Property(vehicle => vehicle.RegistrationExpiry).HasMaxLength(40);
      entity.Property(vehicle => vehicle.RoadTaxExpiry).HasMaxLength(40);
      entity.Property(vehicle => vehicle.InsuranceExpiry).HasMaxLength(40);
      entity.Property(vehicle => vehicle.InsuranceProvider).HasMaxLength(120);
      entity.Property(vehicle => vehicle.InsurancePolicy).HasMaxLength(120);
      entity.Property(vehicle => vehicle.InspectionDue).HasMaxLength(40);
      entity.Property(vehicle => vehicle.AcquiredDate).HasMaxLength(40);
      entity.Property(vehicle => vehicle.Image).HasMaxLength(500);
    });

    ConfigureTripSetupEntity<TripTypeCodeOption>(modelBuilder, "TripTypeCodeOptions");
    ConfigureTripSetupEntity<CargoTypeCodeOption>(modelBuilder, "CargoTypeCodeOptions");
    ConfigureTripSetupEntity<StatusCodeOption>(modelBuilder, "StatusCodeOptions");
    ConfigureTripSetupEntity<TripPriorityCodeOption>(modelBuilder, "TripPriorityCodeOptions");
    ConfigureTripSetupEntity<IncidentTypeCodeOption>(modelBuilder, "IncidentTypeCodeOptions");
    ConfigureTripSetupEntity<SeverityCodeOption>(modelBuilder, "SeverityCodeOptions");
    ConfigureTripSetupEntity<ExpenseTypeCodeOption>(modelBuilder, "ExpenseTypeCodeOptions");
    ConfigureTripSetupEntity<MaintenanceTypeCodeOption>(modelBuilder, "MaintenanceTypeCodeOptions");
    ConfigureTripSetupEntity<DocumentTypeCodeOption>(modelBuilder, "DocumentTypeCodeOptions");
    ConfigureTripSetupEntity<SupplierCodeOption>(modelBuilder, "SupplierCodeOptions");

    modelBuilder.Entity<Trip>(entity =>
    {
      entity.ToTable("Trips");
      entity.HasKey(trip => trip.Id);
      entity.HasIndex(trip => trip.TripNumber).IsUnique();
      entity.Property(trip => trip.TripNumber).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.TripType).HasMaxLength(80).IsRequired();
      entity.Property(trip => trip.Status).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.Priority).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.CustomerName).HasMaxLength(160).IsRequired();
      entity.Property(trip => trip.Department).HasMaxLength(120).IsRequired();
      entity.Property(trip => trip.CostCenter).HasMaxLength(80);
      entity.Property(trip => trip.VehicleId).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.VehiclePlate).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.TrailerNumber).HasMaxLength(80);
      entity.Property(trip => trip.DriverName).HasMaxLength(120).IsRequired();
      entity.Property(trip => trip.CoDriverName).HasMaxLength(120);
      entity.Property(trip => trip.DispatcherName).HasMaxLength(120).IsRequired();
      entity.Property(trip => trip.CargoType).HasMaxLength(120).IsRequired();
      entity.Property(trip => trip.PickupLocation).HasMaxLength(160).IsRequired();
      entity.Property(trip => trip.DropoffLocation).HasMaxLength(160).IsRequired();
      entity.Property(trip => trip.PickupContact).HasMaxLength(160);
      entity.Property(trip => trip.DropoffContact).HasMaxLength(160);
      entity.Property(trip => trip.DepartureDateTime).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.EstimatedArrival).HasMaxLength(40).IsRequired();
      entity.Property(trip => trip.ActualArrival).HasMaxLength(40);
      entity.Property(trip => trip.TemperatureRange).HasMaxLength(80);
      entity.Property(trip => trip.SpecialInstructions).HasMaxLength(1000);
      entity.Property(trip => trip.DriverNotes).HasMaxLength(1000);
    });

    modelBuilder.Entity<MaintenanceTicket>(entity =>
    {
      entity.ToTable("MaintenanceTickets");
      entity.HasKey(ticket => ticket.Id);
      entity.Property(ticket => ticket.Id).HasMaxLength(40);
      entity.Property(ticket => ticket.Vehicle).HasMaxLength(120).IsRequired();
      entity.Property(ticket => ticket.VehicleId).HasMaxLength(80).IsRequired();
      entity.Property(ticket => ticket.Issue).HasMaxLength(160).IsRequired();
      entity.Property(ticket => ticket.Details).HasMaxLength(500).IsRequired();
      entity.Property(ticket => ticket.ReportedDate).HasMaxLength(40).IsRequired();
      entity.Property(ticket => ticket.Mechanic).HasMaxLength(120).IsRequired();
      entity.Property(ticket => ticket.Status).HasMaxLength(30).IsRequired();
      entity.HasIndex(ticket => ticket.Id).IsUnique();
    });

    modelBuilder.Entity<InventoryPart>(entity =>
    {
      entity.ToTable("InventoryParts");
      entity.HasKey(part => part.Id);
      entity.Property(part => part.Id).HasMaxLength(40);
      entity.Property(part => part.Name).HasMaxLength(160).IsRequired();
      entity.Property(part => part.PartNo).HasMaxLength(80).IsRequired();
      entity.Property(part => part.Category).HasMaxLength(120).IsRequired();
      entity.Property(part => part.Supplier).HasMaxLength(160);
      entity.Property(part => part.UnitCost).HasMaxLength(80);
      entity.Property(part => part.Location).HasMaxLength(160);
      entity.Property(part => part.Image).HasMaxLength(500);
      entity.HasIndex(part => part.Id).IsUnique();
      entity.HasIndex(part => part.PartNo);
    });

    modelBuilder.Entity<Incident>(entity =>
    {
      entity.ToTable("Incidents");
      entity.HasKey(incident => incident.Id);
      entity.Property(incident => incident.Id).HasMaxLength(40);
      entity.Property(incident => incident.VehicleId).HasMaxLength(80).IsRequired();
      entity.Property(incident => incident.Driver).HasMaxLength(120).IsRequired();
      entity.Property(incident => incident.Date).HasMaxLength(40).IsRequired();
      entity.Property(incident => incident.Type).HasMaxLength(120).IsRequired();
      entity.Property(incident => incident.Severity).HasMaxLength(40).IsRequired();
      entity.Property(incident => incident.Status).HasMaxLength(40).IsRequired();
      entity.Property(incident => incident.Cost).HasMaxLength(80);
      entity.Property(incident => incident.Notes).HasMaxLength(1000);
      entity.HasIndex(incident => incident.Id).IsUnique();
    });

    modelBuilder.Entity<Expense>(entity =>
    {
      entity.ToTable("Expenses");
      entity.HasKey(expense => expense.Id);
      entity.Property(expense => expense.ExpenseDate).HasMaxLength(40).IsRequired();
      entity.Property(expense => expense.ExpenseType).HasMaxLength(120).IsRequired();
      entity.Property(expense => expense.VehicleId).HasMaxLength(80);
      entity.Property(expense => expense.TripNumber).HasMaxLength(80);
      entity.Property(expense => expense.DriverName).HasMaxLength(120);
      entity.Property(expense => expense.Amount).HasColumnType("decimal(18,2)");
      entity.Property(expense => expense.Status).HasMaxLength(40).IsRequired();
      entity.Property(expense => expense.Notes).HasMaxLength(1000);
    });

    modelBuilder.Entity<FleetDocument>(entity =>
    {
      entity.ToTable("FleetDocuments");
      entity.HasKey(document => document.Id);
      entity.Property(document => document.OwnerType).HasMaxLength(40).IsRequired();
      entity.Property(document => document.OwnerId).HasMaxLength(80).IsRequired();
      entity.Property(document => document.OwnerName).HasMaxLength(160).IsRequired();
      entity.Property(document => document.DocumentType).HasMaxLength(120).IsRequired();
      entity.Property(document => document.DocumentNumber).HasMaxLength(120);
      entity.Property(document => document.IssueDate).HasMaxLength(40);
      entity.Property(document => document.ExpiryDate).HasMaxLength(40);
      entity.Property(document => document.Status).HasMaxLength(40).IsRequired();
      entity.Property(document => document.Notes).HasMaxLength(1000);
    });

    modelBuilder.Entity<AuditLog>(entity =>
    {
      entity.ToTable("AuditLogs");
      entity.HasKey(log => log.Id);
      entity.Property(log => log.RoleId).HasMaxLength(80).IsRequired();
      entity.Property(log => log.ModuleKey).HasMaxLength(80).IsRequired();
      entity.Property(log => log.Action).HasMaxLength(40).IsRequired();
      entity.Property(log => log.EntityId).HasMaxLength(80).IsRequired();
      entity.Property(log => log.Description).HasMaxLength(1000).IsRequired();
    });

    modelBuilder.Entity<StatusHistory>(entity =>
    {
      entity.ToTable("StatusHistories");
      entity.HasKey(history => history.Id);
      entity.Property(history => history.EntityType).HasMaxLength(80).IsRequired();
      entity.Property(history => history.EntityId).HasMaxLength(80).IsRequired();
      entity.Property(history => history.OldStatus).HasMaxLength(80);
      entity.Property(history => history.NewStatus).HasMaxLength(80).IsRequired();
      entity.Property(history => history.RoleId).HasMaxLength(80).IsRequired();
    });
  }

  private static void ConfigureTripSetupEntity<T>(ModelBuilder modelBuilder, string tableName)
    where T : TripSetupCodeOption
  {
    modelBuilder.Entity<T>(entity =>
    {
      entity.ToTable(tableName);
      entity.HasKey(option => option.Id);
      entity.HasIndex(option => option.Name).IsUnique();
      entity.HasIndex(option => option.Code).IsUnique();
      entity.Property(option => option.Name).HasMaxLength(120).IsRequired();
      entity.Property(option => option.Code).HasMaxLength(40).IsRequired();
      entity.Property(option => option.Description).HasMaxLength(500);
      entity.Property(option => option.Status).HasMaxLength(20).IsRequired();
    });
  }
}
