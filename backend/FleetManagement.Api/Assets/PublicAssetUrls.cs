namespace FleetManagement.Api.Assets;

public static class PublicAssetUrls
{
  public static string ToPublicAssetUrl(HttpRequest request, string? path)
  {
    if (string.IsNullOrWhiteSpace(path)) return string.Empty;
    if (path.StartsWith("file:///uploads/", StringComparison.OrdinalIgnoreCase))
    {
      path = path.Replace("file://", "", StringComparison.OrdinalIgnoreCase);
    }

    if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri)) return absoluteUri.ToString();
    if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
    {
      path = $"/{path}";
    }

    if (!path.StartsWith('/')) return path;
    return $"{request.Scheme}://{request.Host}{path}";
  }
}
