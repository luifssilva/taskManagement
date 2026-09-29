using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using TaskManagement.Api.Middleware;
using TaskManagement.Api.Swagger;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums travel by name ("EmProgresso"); numeric values are also accepted on input.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                // Hide serializer exception messages (they expose internal type names).
                options.AllowInputFormatterExceptionMessages = false;
            })
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse);

        return services;
    }

    /// <summary>
    /// Model binding errors (malformed JSON, invalid dates, etc.) use the same format as
    /// business validation errors.
    /// </summary>
    private static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => string.IsNullOrEmpty(entry.Key) ? "body" : entry.Key.TrimStart('$', '.').ToCamelCase(),
                entry => entry.Value!.Errors
                    .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "The value is invalid." : e.ErrorMessage)
                    .ToArray());

        // When the body cannot be deserialized, MVC also reports the body parameter itself as
        // "required"; that entry is noise next to the error that points at the bad field.
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name.ToCamelCase());
        foreach (var name in bodyParameters)
        {
            if (errors.Count > 1)
            {
                errors.Remove(name);
            }
        }

        var problem = ApiProblemDetails.Validation(errors);
        problem.Instance = context.HttpContext.Request.Path;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Task Management API",
                Version = "v1",
                Description = "RESTful API for creating, listing, filtering, searching, updating and deleting tasks."
            });

            IncludeXmlComments(options, typeof(Program).Assembly);
            IncludeXmlComments(options, typeof(TaskResponse).Assembly);

            options.SchemaFilter<TaskStatusSchemaFilter>();
            options.ParameterFilter<CamelCaseQueryParameterFilter>();
        });

        return services;
    }

    private static void IncludeXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, Assembly assembly)
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}
