using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Api.Swagger;

/// <summary>
/// Describes each <see cref="TaskItemStatus"/> value with its id and description from the status domain table.
/// </summary>
public sealed class TaskStatusSchemaFilter : ISchemaFilter
{
    private static readonly string Description =
        "Task status. Allowed values: " +
        string.Join("; ", TaskItemStatusDefinition.All.Select(s => $"{s.Id} ({(int)s.Id}) = {s.Description}")) +
        ". See GET /api/task-statuses.";

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(TaskItemStatus))
        {
            schema.Description = Description;
        }
    }
}
