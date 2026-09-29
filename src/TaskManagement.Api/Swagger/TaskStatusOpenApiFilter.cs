using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Api.Swagger;

/// <summary>
/// Documents the allowed values of every "status" field of the task DTOs, which are
/// transported as strings ("Pendente", "Em progresso", "Concluída").
/// </summary>
public sealed class TaskStatusOpenApiFilter : ISchemaFilter, IParameterFilter
{
    private const string StatusName = "status";

    private static readonly HashSet<Type> TaskDtoTypes =
    [
        typeof(CreateTaskRequest),
        typeof(UpdateTaskRequest),
        typeof(TaskFilterRequest),
        typeof(TaskResponse)
    ];

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (TaskDtoTypes.Contains(context.Type) && schema.Properties.TryGetValue(StatusName, out var statusSchema))
        {
            AddAllowedValues(statusSchema);
        }
    }

    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.In == ParameterLocation.Query &&
            string.Equals(parameter.Name, StatusName, StringComparison.OrdinalIgnoreCase) &&
            parameter.Schema is not null)
        {
            AddAllowedValues(parameter.Schema);
        }
    }

    private static void AddAllowedValues(OpenApiSchema schema)
    {
        schema.Enum = TaskItemStatusExtensions.AllowedDisplayNames
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList();
    }
}
