namespace FleetManagement.Api.Models;

public sealed class User
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string Phone { get; set; }

    public required string Status { get; set; }

    public DateOnly JoinDate { get; set; }

    public string? Avatar { get; set; }

    public int RoleId { get; set; }

    public Role? Role { get; set; }
}
