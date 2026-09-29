using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Tests.Api;

/// <summary>
/// End-to-end checks of the HTTP contract (status codes, Location header and error format).
/// Business rules themselves are covered by the service tests.
/// </summary>
public class TasksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TasksApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidData_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", NewTask());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(created);
        Assert.Equal($"/api/tasks/{created.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Post_WithInvalidData_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", NewTask() with { Title = "", Status = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Validation Error", body.RootElement.GetProperty("title").GetString());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("title", out _));
        Assert.True(errors.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Post_WithMalformedDate_Returns400()
    {
        var json = """{ "title": "Tarefa", "status": "Pendente", "dueDate": "31/12/2026" }""";

        var response = await _client.PostAsync(
            "/api/tasks",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingTask_Returns200()
    {
        var created = await CreateAsync();

        var response = await _client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.Content.ReadFromJsonAsync<TaskResponse>());
    }

    [Fact]
    public async Task Get_MissingTask_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task List_WithFilters_ReturnsOnlyMatchingTasks()
    {
        var dueDate = new DateOnly(2031, 1, 15);
        var match = await CreateAsync(NewTask() with { DueDate = dueDate, Status = "Em progresso" });
        await CreateAsync(NewTask() with { DueDate = dueDate, Status = "Pendente" });

        var tasks = await _client.GetFromJsonAsync<List<TaskResponse>>(
            "/api/tasks?status=Em%20progresso&dueDate=2031-01-15");

        var task = Assert.Single(tasks!);
        Assert.Equal(match.Id, task.Id);
    }

    [Fact]
    public async Task List_WithInvalidDate_Returns400()
    {
        var response = await _client.GetAsync("/api/tasks?dueDate=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsMatchingTasks()
    {
        var unique = $"termo-{Guid.NewGuid():N}";
        var created = await CreateAsync(NewTask() with { Description = $"Descrição com {unique}" });

        var tasks = await _client.GetFromJsonAsync<List<TaskResponse>>($"/api/tasks/search?term={unique}");

        Assert.Equal(created.Id, Assert.Single(tasks!).Id);
    }

    [Fact]
    public async Task Search_WithoutTerm_Returns400()
    {
        var response = await _client.GetAsync("/api/tasks/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ExistingTask_Returns200WithUpdatedData()
    {
        var created = await CreateAsync();
        var update = new UpdateTaskRequest { Title = "Atualizada", Status = "Concluída" };

        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("Atualizada", updated!.Title);
        Assert.Equal("Concluída", updated.Status);
    }

    [Fact]
    public async Task Put_MissingTask_Returns404()
    {
        var update = new UpdateTaskRequest { Title = "Atualizada", Status = "Concluída" };

        var response = await _client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithInvalidData_Returns400()
    {
        var created = await CreateAsync();

        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}", new UpdateTaskRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204ThenGetReturns404()
    {
        var created = await CreateAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{created.Id}");
        var getResponse = await _client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingTask_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocument_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static CreateTaskRequest NewTask() => new()
    {
        Title = "Tarefa de integração",
        Description = "Criada pelos testes de API",
        DueDate = new DateOnly(2026, 10, 1),
        Status = "Pendente"
    };

    private async Task<TaskResponse> CreateAsync(CreateTaskRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", request ?? NewTask());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }
}
