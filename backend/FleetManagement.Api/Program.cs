using FleetManagement.Api.Assets;
using FleetManagement.Api.Data;
using FleetManagement.Api.Email;
using FleetManagement.Api.Endpoints;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FleetDbContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("FleetDatabase")));

builder.Services.AddCors(options =>
{
  options.AddPolicy("frontend", policy =>
    policy
      .WithOrigins("http://localhost:5173")
      .AllowAnyHeader()
      .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<OtpChallengeStore>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();

app.UseCors("frontend");
app.Use(async (context, next) =>
{
  var token = JwtTokenService.GetBearerToken(context.Request);
  if (JwtTokenService.TryValidateToken(context.RequestServices.GetRequiredService<IConfiguration>(), token, out var user))
  {
    context.Request.Headers.TryAdd("X-Fleet-User-Id", user.UserId);
    context.Request.Headers.TryAdd("X-Fleet-User-Name", user.Name);
    context.Request.Headers.TryAdd("X-Fleet-Role-Id", user.RoleId);
  }

  await next();
});
app.UseStaticFiles();
var uploadsRoot = UserAssetStorage.GetUploadsRootPath(app.Environment);
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(uploadsRoot),
  RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
  var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
  await SchemaBootstrapper.EnsureRolesSchemaAsync(db);
  if (app.Configuration.GetValue("SeedData:Enabled", true))
  {
    await SeedData.InitializeAsync(db);
  }
  await UserAssetStorage.RepairStoredUserAssetPathsAsync(db, environment);
}

app.MapDashboardEndpoints();
app.MapReportsEndpoints();
app.MapAuthEndpoints();
app.MapAuditEndpoints();
app.MapRolesEndpoints();
app.MapPermissionsEndpoints();
app.MapTripSetupEndpoints();
app.MapExpensesEndpoints();
app.MapInventoryEndpoints();
app.MapMaintenanceEndpoints();
app.MapIncidentsEndpoints();
app.MapTripsEndpoints();
app.MapDepartmentsEndpoints();
app.MapSetupOptionEndpoints();
app.MapLocationsEndpoints();
app.MapVehiclesEndpoints();
app.MapUsersEndpoints();

app.Run();
