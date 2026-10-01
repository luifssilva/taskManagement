using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.Api.Extensions;

/// <summary>
/// Builds validation error responses so that model binding errors and business validation
/// errors share the same shape.
/// </summary>
internal static class ApiProblemDetails
{
    public static ValidationProblemDetails Validation(IDictionary<string, string[]> errors) =>
        new(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "The request contains invalid data."
        };

    public static string ToCamelCase(this string value) =>
        string.IsNullOrEmpty(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
}
