using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Data;

public static class UserAssetStorage
{
  private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
  {
    ".jpg",
    ".jpeg",
    ".png",
    ".webp",
    ".gif"
  };

  public static async Task<string> SaveImageAsync(
    IFormFile file,
    string userId,
    string assetName,
    IWebHostEnvironment environment)
  {
    return await SaveImageAsync(file, "users", userId, assetName, environment);
  }

  public static async Task<string> SaveImageAsync(
    IFormFile file,
    string entityFolder,
    string entityId,
    string assetName,
    IWebHostEnvironment environment)
  {
    if (file.Length <= 0) throw new InvalidOperationException("Uploaded file is empty.");
    if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Only image uploads are allowed.");
    }

    var extension = Path.GetExtension(file.FileName);
    if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
    {
      throw new InvalidOperationException("Unsupported image format.");
    }

    var userDirectory = GetEntityDirectoryPath(entityFolder, entityId, environment);

    Directory.CreateDirectory(userDirectory);

    foreach (var existingFile in Directory.GetFiles(userDirectory, $"{assetName}.*"))
    {
      File.Delete(existingFile);
    }

    var fileName = $"{assetName}{extension.ToLowerInvariant()}";
    var fullPath = Path.Combine(userDirectory, fileName);

    await using var stream = File.Create(fullPath);
    await file.CopyToAsync(stream);

    return $"/uploads/{entityFolder}/{entityId}/{fileName}";
  }

  public static async Task RepairStoredUserAssetPathsAsync(FleetDbContext db, IWebHostEnvironment environment)
  {
    var users = await db.Users
      .Where(u => u.IsDeleted == 0)
      .ToListAsync();

    var hasChanges = false;

    foreach (var user in users)
    {
      var avatar = ResolveStoredPath(user.Avatar, user.Id, "avatar", environment);
      var nrcFront = ResolveStoredPath(user.NrcFront, user.Id, "nrc-front", environment);
      var nrcBack = ResolveStoredPath(user.NrcBack, user.Id, "nrc-back", environment);

      if (!string.Equals(user.Avatar, avatar, StringComparison.Ordinal))
      {
        user.Avatar = avatar;
        hasChanges = true;
      }

      if (!string.Equals(user.NrcFront, nrcFront, StringComparison.Ordinal))
      {
        user.NrcFront = nrcFront;
        hasChanges = true;
      }

      if (!string.Equals(user.NrcBack, nrcBack, StringComparison.Ordinal))
      {
        user.NrcBack = nrcBack;
        hasChanges = true;
      }
    }

    if (hasChanges)
    {
      await db.SaveChangesAsync();
    }
  }

  public static string ResolveStoredPath(string? storedPath, string userId, string assetName, IWebHostEnvironment environment)
  {
    if (string.IsNullOrWhiteSpace(storedPath))
    {
      return FindExistingAssetPath(userId, assetName, environment) ?? string.Empty;
    }

    if (storedPath.StartsWith("file:///uploads/", StringComparison.OrdinalIgnoreCase))
    {
      storedPath = storedPath.Replace("file://", "", StringComparison.OrdinalIgnoreCase);
    }

    if (storedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        storedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
      return storedPath;
    }

    var normalizedPath = storedPath.Replace('\\', '/');
    var rootPath = GetWebRootPath(environment);
    var relativePath = normalizedPath.TrimStart('/');
    var fullPath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    if (File.Exists(fullPath))
    {
      return normalizedPath.StartsWith('/') ? normalizedPath : $"/{normalizedPath}";
    }

    return FindExistingAssetPath(userId, assetName, environment) ?? (normalizedPath.StartsWith('/') ? normalizedPath : $"/{normalizedPath}");
  }

  private static string? FindExistingAssetPath(string userId, string assetName, IWebHostEnvironment environment)
  {
    var userDirectory = GetUserDirectoryPath(userId, environment);

    if (!Directory.Exists(userDirectory)) return null;

    var filePath = Directory
      .GetFiles(userDirectory, $"{assetName}.*")
      .OrderBy(path => path)
      .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(filePath)) return null;

    var fileName = Path.GetFileName(filePath);
    return $"/uploads/users/{userId}/{fileName}";
  }

  public static string GetWebRootPath(IWebHostEnvironment environment)
  {
    if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
    {
      return environment.WebRootPath;
    }

    var baseDirectoryWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
    if (Directory.Exists(baseDirectoryWebRoot))
    {
      return baseDirectoryWebRoot;
    }

    return Path.Combine(environment.ContentRootPath, "wwwroot");
  }

  public static string GetUploadsRootPath(IWebHostEnvironment environment) =>
    Path.Combine(GetWebRootPath(environment), "uploads");

  private static string GetUserDirectoryPath(string userId, IWebHostEnvironment environment) =>
    GetEntityDirectoryPath("users", userId, environment);

  private static string GetEntityDirectoryPath(string entityFolder, string entityId, IWebHostEnvironment environment) =>
    Path.Combine(GetUploadsRootPath(environment), entityFolder, entityId);
}
