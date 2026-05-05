namespace FleetManagement.Api.Dtos;

public record DashboardMetricDto(string Title, int Value, string Icon, string Tone);

public record NamedCountDto(string Name, int Value);

public record DashboardRecentTripDto(
  int Id,
  string TripNumber,
  string Vehicle,
  string Driver,
  string Route,
  string Status,
  string Details);

public record DashboardUpcomingExpiryDto(
  string Source,
  string Label,
  string Date,
  int DaysRemaining);

public record DashboardSummaryDto(
  IReadOnlyList<DashboardMetricDto> Metrics,
  IReadOnlyList<NamedCountDto> VehicleStatuses,
  IReadOnlyList<NamedCountDto> TripStatuses,
  IReadOnlyList<DashboardRecentTripDto> RecentTrips,
  IReadOnlyList<DashboardUpcomingExpiryDto> UpcomingExpiries);
