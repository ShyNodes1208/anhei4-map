namespace Anhei4Map.Core.Services;

public static class DomainPolicy
{
    public static bool IsAllowed(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        Uri parsed;
        try
        {
            parsed = new Uri(uri, UriKind.Absolute);
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!parsed.IsDefaultPort && parsed.Port != 443)
        {
            return false;
        }

        var host = parsed.Host.ToLowerInvariant();
        return host is "helltides.com" or "www.helltides.com";
    }
}
