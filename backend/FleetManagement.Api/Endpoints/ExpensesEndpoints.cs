using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class ExpensesEndpoints
{
  public static IEndpointRouteBuilder MapExpensesEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/expenses", async (
      HttpRequest httpRequest,
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? status = null,
      string? dateFrom = null,
      string? dateTo = null) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.View);
      if (permissionError is not null) return permissionError;

      var query = db.Expenses.Where(expense => expense.IsDeleted == 0).AsNoTracking().AsQueryable();
      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(expense =>
          expense.ExpenseType.ToLower().Contains(normalizedSearch) ||
          (expense.VehicleId ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          (expense.TripNumber ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          (expense.DriverName ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          (expense.Notes ?? string.Empty).ToLower().Contains(normalizedSearch));
      }
      if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(expense => expense.Status == status);
      if (!string.IsNullOrWhiteSpace(dateFrom)) query = query.Where(expense => string.Compare(expense.ExpenseDate, dateFrom) >= 0);
      if (!string.IsNullOrWhiteSpace(dateTo)) query = query.Where(expense => string.Compare(expense.ExpenseDate, dateTo) <= 0);

      var total = await query.CountAsync();
      var records = await query.OrderByDescending(expense => expense.ExpenseDate).Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
      return Results.Ok(new PagedResult<ExpenseDto>(records.Select(ToExpenseDto).ToList(), total));
    });

    app.MapPost("/api/expenses", async (ExpenseRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.Create);
      if (permissionError is not null) return permissionError;
      var validationError = ValidateExpenseRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var now = DateTime.UtcNow;
      var expense = new Expense
      {
        ExpenseDate = request.ExpenseDate.Trim(),
        ExpenseType = request.ExpenseType.Trim(),
        VehicleId = NormalizeOptional(request.VehicleId),
        TripNumber = NormalizeOptional(request.TripNumber),
        DriverName = NormalizeOptional(request.DriverName),
        Amount = request.Amount,
        Status = request.Status.Trim(),
        Notes = NormalizeOptional(request.Notes),
        CreatedAt = now,
        UpdatedAt = now
      };
      db.Expenses.Add(expense);
      await db.SaveChangesAsync();
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "expenses", "Create", expense.Id.ToString(), $"Created expense {expense.ExpenseType}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToExpenseDto(expense));
    });

    app.MapPut("/api/expenses/{id:int}", async (int id, ExpenseRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;
      var validationError = ValidateExpenseRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var expense = await db.Expenses.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (expense is null) return Results.NotFound(new ApiError("Expense not found."));
      var oldStatus = expense.Status;
      expense.ExpenseDate = request.ExpenseDate.Trim();
      expense.ExpenseType = request.ExpenseType.Trim();
      expense.VehicleId = NormalizeOptional(request.VehicleId);
      expense.TripNumber = NormalizeOptional(request.TripNumber);
      expense.DriverName = NormalizeOptional(request.DriverName);
      expense.Amount = request.Amount;
      expense.Status = request.Status.Trim();
      expense.Notes = NormalizeOptional(request.Notes);
      expense.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "Expense", expense.Id.ToString(), oldStatus, expense.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "expenses", "Edit", expense.Id.ToString(), $"Updated expense {expense.ExpenseType}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToExpenseDto(expense));
    });

    app.MapDelete("/api/expenses/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;
      var expense = await db.Expenses.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (expense is null) return Results.NotFound(new ApiError("Expense not found."));
      expense.IsDeleted = 1;
      expense.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "expenses", "Delete", expense.Id.ToString(), $"Deleted expense {expense.ExpenseType}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static ExpenseDto ToExpenseDto(Expense expense) =>
    new(
      expense.Id,
      expense.ExpenseDate,
      expense.ExpenseType,
      expense.VehicleId,
      expense.TripNumber,
      expense.DriverName,
      expense.Amount,
      expense.Status,
      expense.Notes,
      expense.CreatedAt,
      expense.UpdatedAt);

  private static string? ValidateExpenseRequest(ExpenseRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.ExpenseDate)) return "Expense date is required.";
    if (string.IsNullOrWhiteSpace(request.ExpenseType)) return "Expense type is required.";
    if (request.Amount < 0) return "Expense amount cannot be negative.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
    if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000) return "Notes must be 1000 characters or fewer.";
    return null;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
