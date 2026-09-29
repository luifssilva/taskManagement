using TaskManagement.Api.Extensions;
using TaskManagement.Application;
using TaskManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddErrorHandling()
    .AddApiControllers()
    .AddSwaggerDocumentation();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger is enabled in every environment so the API can be explored right after `dotnet run`.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
    options.DocumentTitle = "Task Management API";
});

// In Development the plain "http" launch profile has no HTTPS port to redirect to.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();

// Exposes the entry point to integration tests (WebApplicationFactory).
public partial class Program;
