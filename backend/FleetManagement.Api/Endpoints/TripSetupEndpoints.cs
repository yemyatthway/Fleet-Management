using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class TripSetupEndpoints
{
  public static IEndpointRouteBuilder MapTripSetupEndpoints(this IEndpointRouteBuilder app)
  {
    MapSetup<TripTypeCodeOption>(app, "/api/trip-types", "trip-type-setup");
    MapSetup<CargoTypeCodeOption>(app, "/api/cargo-types", "cargo-type-setup");
    MapSetup<StatusCodeOption>(app, "/api/statuses", "status-setup");
    MapSetup<TripPriorityCodeOption>(app, "/api/trip-priorities", "trip-priority-setup");
    MapSetup<IncidentTypeCodeOption>(app, "/api/incident-types", "incident-type-setup");
    MapSetup<SeverityCodeOption>(app, "/api/severities", "severity-setup");
    MapSetup<ExpenseTypeCodeOption>(app, "/api/expense-types", "expense-type-setup");
    MapSetup<MaintenanceTypeCodeOption>(app, "/api/maintenance-types", "maintenance-type-setup");
    MapSetup<DocumentTypeCodeOption>(app, "/api/document-types", "document-type-setup");
    MapSetup<SupplierCodeOption>(app, "/api/suppliers", "supplier-setup");
    return app;
  }

  private static void MapSetup<T>(IEndpointRouteBuilder app, string path, string moduleKey)
    where T : TripSetupCodeOption, new()
  {
    app.MapGet(path, async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
      Results.Ok(await GetTripSetupPage<T>(db, page, pageSize, search, sortBy, sortOrder)));
    app.MapGet($"{path}/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<T>(db)));
    app.MapPost(path, async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<T>(request, httpRequest, db, moduleKey));
    app.MapPut($"{path}/{{id:int}}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<T>(id, request, httpRequest, db, moduleKey));
    app.MapDelete($"{path}/{{id:int}}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<T>(id, httpRequest, db, moduleKey));
  }

  private static TripSetupDto ToTripSetupDto(TripSetupCodeOption option) =>
    new(option.Id, option.Name, option.Code, option.Description, option.Status, option.CreatedAt, option.UpdatedAt);

  private static string? ValidateTripSetupRequest(TripSetupRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
    if (request.Name.Trim().Length > 120) return "Name must be 120 characters or fewer.";
    if (string.IsNullOrWhiteSpace(request.Code)) return "Code is required.";
    if (request.Code.Trim().Length > 40) return "Code must be 40 characters or fewer.";
    if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500) return "Description must be 500 characters or fewer.";
    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
    return normalizedStatus is "Active" or "Disabled" ? null : "Status must be Active or Disabled.";
  }

  private static async Task<PagedResult<TripSetupDto>> GetTripSetupPage<T>(
    FleetDbContext db,
    int page,
    int pageSize,
    string? search,
    string? sortBy,
    string? sortOrder)
    where T : TripSetupCodeOption
  {
    var query = db.Set<T>().AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
      var normalizedSearch = search.Trim().ToLower();
      query = query.Where(option =>
        option.Name.ToLower().Contains(normalizedSearch) ||
        option.Code.ToLower().Contains(normalizedSearch) ||
        (option.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
        option.Status.ToLower().Contains(normalizedSearch));
    }

    query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
    {
      ("name", "desc") => query.OrderByDescending(option => option.Name),
      ("name", _) => query.OrderBy(option => option.Name),
      ("code", "desc") => query.OrderByDescending(option => option.Code),
      ("code", _) => query.OrderBy(option => option.Code),
      ("status", "desc") => query.OrderByDescending(option => option.Status),
      ("status", _) => query.OrderBy(option => option.Status),
      ("id", "desc") => query.OrderByDescending(option => option.Id),
      _ => query.OrderBy(option => option.Id)
    };

    var total = await query.CountAsync();
    var records = await query.Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
    return new PagedResult<TripSetupDto>(records.Select(ToTripSetupDto).ToList(), total);
  }

  private static async Task<List<string>> GetTripSetupOptions<T>(FleetDbContext db)
    where T : TripSetupCodeOption =>
    await db.Set<T>()
      .AsNoTracking()
      .Where(option => option.Status == "Active")
      .OrderBy(option => option.Name)
      .Select(option => option.Name)
      .ToListAsync();

  private static async Task<IResult> CreateTripSetupOption<T>(TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db, string moduleKey)
    where T : TripSetupCodeOption, new()
  {
    var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Create);
    if (permissionError is not null) return permissionError;

    var validationError = ValidateTripSetupRequest(request);
    if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
    var normalizedName = request.Name.Trim();
    var normalizedCode = request.Code.Trim();
    var duplicate = await db.Set<T>().AnyAsync(option => option.Name.ToLower() == normalizedName.ToLower() || option.Code.ToLower() == normalizedCode.ToLower());
    if (duplicate) return Results.BadRequest(new ApiError("Name or code already exists."));
    var option = new T
    {
      Name = normalizedName,
      Code = normalizedCode,
      Description = NormalizeOptional(request.Description),
      Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
      CreatedAt = DateTimeOffset.UtcNow
    };
    db.Set<T>().Add(option);
    await AuditLogWriter.LogAuditAsync(db, httpRequest, moduleKey, "Create", normalizedCode, $"Created setup option {normalizedName}.");
    await db.SaveChangesAsync();
    return Results.Ok(ToTripSetupDto(option));
  }

  private static async Task<IResult> UpdateTripSetupOption<T>(int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db, string moduleKey)
    where T : TripSetupCodeOption
  {
    var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Edit);
    if (permissionError is not null) return permissionError;

    var validationError = ValidateTripSetupRequest(request);
    if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
    var option = await db.Set<T>().FindAsync(id);
    if (option is null) return Results.NotFound(new ApiError("Setup option not found."));
    var normalizedName = request.Name.Trim();
    var normalizedCode = request.Code.Trim();
    var duplicate = await db.Set<T>().AnyAsync(item => item.Id != id && (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
    if (duplicate) return Results.BadRequest(new ApiError("Name or code already exists."));
    option.Name = normalizedName;
    option.Code = normalizedCode;
    option.Description = NormalizeOptional(request.Description);
    option.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
    option.UpdatedAt = DateTimeOffset.UtcNow;
    await AuditLogWriter.LogAuditAsync(db, httpRequest, moduleKey, "Edit", option.Id.ToString(), $"Updated setup option {normalizedName}.");
    await db.SaveChangesAsync();
    return Results.Ok(ToTripSetupDto(option));
  }

  private static async Task<IResult> DeleteTripSetupOption<T>(int id, HttpRequest httpRequest, FleetDbContext db, string moduleKey)
    where T : TripSetupCodeOption
  {
    var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Delete);
    if (permissionError is not null) return permissionError;

    var option = await db.Set<T>().FindAsync(id);
    if (option is null) return Results.NotFound(new ApiError("Setup option not found."));
    await AuditLogWriter.LogAuditAsync(db, httpRequest, moduleKey, "Delete", option.Id.ToString(), $"Deleted setup option {option.Name}.");
    db.Set<T>().Remove(option);
    await db.SaveChangesAsync();
    return Results.NoContent();
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
