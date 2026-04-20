namespace FleetManagement.Api.Models;

public sealed class User
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string EmployeeId { get; set; }

    public required string NrcNumber { get; set; }

    public required string Email { get; set; }

    public required string Phone { get; set; }

    public required string Status { get; set; }

    public DateOnly JoinDate { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public string? Avatar { get; set; }

    public string? NrcFront { get; set; }

    public string? NrcBack { get; set; }

    public required string Department { get; set; }

    public required string Title { get; set; }

    public required string Location { get; set; }

    public required string Manager { get; set; }

    public string? LicenseNumber { get; set; }

    public string? LicenseClass { get; set; }

    public DateOnly? LicenseExpiry { get; set; }

    public required string EmergencyContactName { get; set; }

    public required string EmergencyContactRelation { get; set; }

    public required string EmergencyContactPhone { get; set; }

    public required string Address { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public string? Notes { get; set; }

    public int RoleId { get; set; }

    public Role? Role { get; set; }
}
