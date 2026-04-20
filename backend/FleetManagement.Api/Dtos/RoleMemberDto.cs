namespace FleetManagement.Api.Dtos;

public sealed record RoleMemberDto(
    int Id,
    string Name,
    string Email,
    string Phone,
    string Status,
    DateOnly JoinDate,
    string? Avatar);
