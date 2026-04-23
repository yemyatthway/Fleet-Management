namespace FleetManagement.Api.Dtos;

public sealed record UserCodeOptionRequest(
    string Type,
    string Name,
    string? Code,
    string? LocationType,
    string? Address,
    string? City,
    string? Country,
    string? ContactPerson,
    string? Phone,
    string? OperatingHours,
    string? Description,
    string Status);
