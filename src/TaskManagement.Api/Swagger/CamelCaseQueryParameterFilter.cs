using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaskManagement.Api.Extensions;

namespace TaskManagement.Api.Swagger;

/// <summary>
/// Documents query parameters bound from DTO properties (e.g. <c>Status</c>) in camelCase,
/// matching the JSON naming. Query string binding is case-insensitive, so both forms work.
/// </summary>
public sealed class CamelCaseQueryParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.In == ParameterLocation.Query)
        {
            parameter.Name = parameter.Name.ToCamelCase();
        }
    }
}
