namespace FleetManagement.Api.Dtos;

public record FleetDocumentDto(
  int Id,
  string OwnerType,
  string OwnerId,
  string OwnerName,
  string DocumentType,
  string? DocumentNumber,
  string? IssueDate,
  string? ExpiryDate,
  string Status,
  string? Notes,
  DateTime CreatedAt,
  DateTime UpdatedAt);

public record FleetDocumentRequest(
  string OwnerType,
  string OwnerId,
  string OwnerName,
  string DocumentType,
  string? DocumentNumber,
  string? IssueDate,
  string? ExpiryDate,
  string Status,
  string? Notes);
