using Microsoft.AspNetCore.Mvc;

namespace MakauTech.Helpers;

public static class PlaceImageUrl
{
    public static string Resolve(IUrlHelper url, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return url.Content("~/Images/placeholder.svg");

        var u = imageUrl.Trim();
        if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || u.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return u;

        if (u.StartsWith("~/", StringComparison.Ordinal))
            return url.Content(u);

        if (u.StartsWith('/'))
            return url.Content("~" + u);

        return url.Content("~/" + u.TrimStart('/'));
    }
}
