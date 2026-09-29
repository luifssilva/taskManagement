using System.Globalization;

namespace TaskManagement.Api.Http;

/// <summary>
/// Converts a task version to an ETag and reads the expected version from an If-Match header.
/// </summary>
public static class TaskETag
{
    public static string From(int version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    /// <summary>
    /// Returns the version the client expects, or <c>null</c> when no precondition applies
    /// (header absent or <c>*</c>, which only requires the task to exist).
    /// </summary>
    /// <remarks>
    /// A value that is not an ETag issued by this API can never match, so it maps to version 0
    /// (versions start at 1) and the request fails with 412, as RFC 9110 prescribes.
    /// </remarks>
    public static int? ExpectedVersion(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch) || ifMatch.Trim() == "*")
        {
            return null;
        }

        var value = ifMatch.Trim();
        if (value.StartsWith("W/", StringComparison.Ordinal))
        {
            value = value[2..];
        }

        return int.TryParse(value.Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out var version)
            ? version
            : 0;
    }
}
