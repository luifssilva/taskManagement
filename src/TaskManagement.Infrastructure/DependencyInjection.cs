using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultDatabaseName = "TaskManagementDb";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databaseName = DefaultDatabaseName)
    {
        services.AddDbContext<TaskDbContext>(options => options.UseInMemoryDatabase(databaseName));

        // One repository instance per request serves both the read and the write abstractions.
        services.AddScoped<TaskRepository>();
        services.AddScoped<ITaskReadRepository>(sp => sp.GetRequiredService<TaskRepository>());
        services.AddScoped<ITaskWriteRepository>(sp => sp.GetRequiredService<TaskRepository>());
        services.AddScoped<ITaskStatusReadRepository, TaskStatusRepository>();

        return services;
    }

    /// <summary>
    /// Creates the database and seeds the status domain table.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
