namespace FleetManagement.Api.Dtos;

public sealed record RoleDto(
    int Id,
    string Name,
    string Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int Members);
