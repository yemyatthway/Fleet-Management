namespace FleetManagement.Api.Dtos;

public record UserPagedResult(IReadOnlyList<UserDto> Items, int Total, UserStatsDto Stats);
