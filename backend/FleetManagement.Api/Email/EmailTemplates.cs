using System.Net;
using System.Globalization;
using FleetManagement.Api.Models;

namespace FleetManagement.Api.Email;

public static class EmailTemplates
{
  public static string OtpCode(string recipientName, string code, int expiresInMinutes = 10) =>
    Layout(
      "Login verification",
      "FleetManager",
      "Login verification",
      $"""
      <p class="lead">Hi {Encode(recipientName)},</p>
      <p>Use this verification code to finish signing in to FleetManager.</p>
      <div class="otp-code" aria-label="Verification code">{Encode(code)}</div>
      <p class="muted">This code expires in {expiresInMinutes} minutes. If you did not request it, you can safely ignore this email.</p>
      """);

  public static string TripAssignment(User driver, Trip trip, IReadOnlyList<string>? changes = null) =>
    TripNotification(
      "New trip assignment",
      $"You have been assigned to trip {Encode(trip.TripNumber)}.",
      driver,
      trip,
      changes,
      "Please open FleetManager to review the trip details.");

  public static string TripUpdated(User driver, Trip trip, string updatedBy, IReadOnlyList<string> changes) =>
    TripNotification(
      "Trip updated",
      $"Your assigned trip {Encode(trip.TripNumber)} has been updated by {Encode(updatedBy)}.",
      driver,
      trip,
      changes,
      "Please open FleetManager to review the latest trip details.");

  private static string TripNotification(
    string title,
    string message,
    User driver,
    Trip trip,
    IReadOnlyList<string>? changes,
    string footerText)
  {
    var changesHtml = changes is { Count: > 0 }
      ? $"""
        <div class="section">
          <h2>Changes</h2>
          <ul class="change-list">{string.Join("", changes.Select(ChangeItem))}</ul>
        </div>
        """
      : string.Empty;

    return Layout(
      title,
      "FleetManager Trips",
      title,
      $"""
      <p class="lead">Hi {Encode(driver.Name)},</p>
      <p>{message}</p>
      <div class="summary">
        <div>
          <span>Trip</span>
          <strong>{Encode(trip.TripNumber)}</strong>
        </div>
        <div>
          <span>Status</span>
          <strong>{Encode(trip.Status)}</strong>
        </div>
        <div>
          <span>Priority</span>
          <strong>{Encode(trip.Priority)}</strong>
        </div>
      </div>
      <div class="section">
        <h2>Trip details</h2>
        <table role="presentation" class="details">
          {DetailRow("Vehicle", $"{trip.VehiclePlate} ({trip.VehicleId})")}
          {DetailRow("Route", $"{trip.PickupLocation} to {trip.DropoffLocation}")}
          {DetailRow("Departure", FormatTripDateTime(trip.DepartureDateTime))}
          {DetailRow("ETA", FormatTripDateTime(trip.EstimatedArrival))}
        </table>
      </div>
      {changesHtml}
      <p class="muted">{Encode(footerText)}</p>
      """);
  }

  private static string Layout(string title, string preheader, string eyebrow, string content) =>
    $$"""
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <style>
        body { margin: 0; background: #f4f7fb; color: #102033; font-family: Arial, Helvetica, sans-serif; }
        .preheader { display: none; max-height: 0; overflow: hidden; opacity: 0; color: transparent; }
        .wrap { width: 100%; padding: 32px 12px; background: #f4f7fb; }
        .card { max-width: 640px; margin: 0 auto; background: #ffffff; border: 1px solid #dbe4ef; border-radius: 8px; overflow: hidden; }
        .header { padding: 26px 30px; background: #0f766e; color: #ffffff; }
        .brand { font-size: 13px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; opacity: .88; }
        h1 { margin: 10px 0 0; font-size: 26px; line-height: 1.25; font-weight: 700; }
        h2 { margin: 0 0 12px; font-size: 15px; color: #102033; }
        .body { padding: 30px; }
        p { margin: 0 0 16px; font-size: 15px; line-height: 1.6; }
        .lead { font-size: 17px; color: #102033; }
        .muted { color: #64748b; font-size: 13px; }
        .otp-code { margin: 22px 0; padding: 18px 20px; border-radius: 8px; background: #ecfeff; border: 1px solid #99f6e4; color: #134e4a; font-size: 34px; line-height: 1; font-weight: 800; letter-spacing: .22em; text-align: center; }
        .summary { display: table; width: 100%; margin: 22px 0; border: 1px solid #dbe4ef; border-radius: 8px; overflow: hidden; }
        .summary div { display: table-cell; width: 33.33%; padding: 14px; border-right: 1px solid #dbe4ef; background: #f8fafc; }
        .summary div:last-child { border-right: 0; }
        .summary span { display: block; margin-bottom: 6px; color: #64748b; font-size: 12px; text-transform: uppercase; }
        .summary strong { display: block; color: #102033; font-size: 15px; }
        .section { margin-top: 24px; }
        .details { width: 100%; border-collapse: collapse; border: 1px solid #dbe4ef; border-radius: 8px; overflow: hidden; }
        .details th, .details td { padding: 12px 14px; border-bottom: 1px solid #e5edf5; font-size: 14px; text-align: left; vertical-align: top; }
        .details th { width: 34%; color: #64748b; font-weight: 700; background: #f8fafc; }
        .details tr:last-child th, .details tr:last-child td { border-bottom: 0; }
        .change-list { margin: 0; padding: 0; list-style: none; border: 1px solid #dbe4ef; border-radius: 8px; overflow: hidden; }
        .change-list li { padding: 12px 14px; border-bottom: 1px solid #e5edf5; font-size: 14px; line-height: 1.5; }
        .change-list li:last-child { border-bottom: 0; }
        .footer { max-width: 640px; margin: 14px auto 0; color: #64748b; font-size: 12px; text-align: center; }
        @media (max-width: 520px) {
          .wrap { padding: 18px 8px; }
          .header, .body { padding: 22px; }
          h1 { font-size: 22px; }
          .summary, .summary div { display: block; width: auto; }
          .summary div { border-right: 0; border-bottom: 1px solid #dbe4ef; }
          .summary div:last-child { border-bottom: 0; }
        }
      </style>
    </head>
    <body>
      <span class="preheader">{{Encode(preheader)}}</span>
      <div class="wrap">
        <div class="card">
          <div class="header">
            <div class="brand">FleetManager</div>
            <h1>{{Encode(title)}}</h1>
          </div>
          <div class="body">
            {{content}}
          </div>
        </div>
        <div class="footer">{{Encode(eyebrow)}} from FleetManager</div>
      </div>
    </body>
    </html>
    """;

  private static string DetailRow(string label, string? value) =>
    $"<tr><th>{Encode(label)}</th><td>{Encode(value)}</td></tr>";

  private static string ChangeItem(string change)
  {
    var normalized = change.TrimStart('-', ' ');
    return $"<li>{Encode(normalized)}</li>";
  }

  private static string Encode(string? value) =>
    WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "Not set" : value.Trim());

  private static string FormatTripDateTime(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return "Not set";

    if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) &&
        !DateTime.TryParse(value, out parsed))
    {
      return value.Trim();
    }

    var localTime = parsed.Kind == DateTimeKind.Utc
      ? TimeZoneInfo.ConvertTimeFromUtc(parsed, MyanmarTimeZone)
      : parsed;

    return localTime.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) + " Myanmar Time";
  }

  private static readonly TimeZoneInfo MyanmarTimeZone = ResolveMyanmarTimeZone();

  private static TimeZoneInfo ResolveMyanmarTimeZone()
  {
    try
    {
      return TimeZoneInfo.FindSystemTimeZoneById("Asia/Rangoon");
    }
    catch (TimeZoneNotFoundException)
    {
      return TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time");
    }
  }
}
