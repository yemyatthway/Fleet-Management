namespace FleetManagement.Api.Dtos;

public record RoleMemberDto(
  string Id,
  string Name,
  string Email,
  string Phone,
  string Status,
  string JoinDate,
  string Avatar);
