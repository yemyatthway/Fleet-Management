namespace FleetManagement.Api.Dtos;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total);
