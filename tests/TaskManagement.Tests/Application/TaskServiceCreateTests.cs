using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

public class TaskServiceCreateTests
{
    private readonly TaskServiceBuilder _builder = new();

    [Fact]
    public async Task CreateAsync_WithValidData_PersistsAndReturnsTask()
    {
        var service = _builder.Build();
        var request = TaskServiceBuilder.ValidCreateRequest();

        var response = await service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Implementar API", response.Title);
        Assert.Equal("Criar endpoints de tarefas", response.Description);
        Assert.Equal(new DateOnly(2026, 10, 1), response.DueDate);
        Assert.Equal("Pendente", response.Status);
        Assert.Equal(TaskServiceBuilder.Now, response.CreatedAt);
        Assert.Null(response.UpdatedAt);
        Assert.True(await _builder.Context.Tasks.AnyAsync(t => t.Id == response.Id));
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueIdentifiers()
    {
        var service = _builder.Build();

        var first = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());
        var second = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateAsync_WithOnlyRequiredFields_Succeeds()
    {
        var service = _builder.Build();

        var response = await service.CreateAsync(new CreateTaskRequest { Title = "Tarefa", Status = "Concluída" });

        Assert.Null(response.Description);
        Assert.Null(response.DueDate);
        Assert.Equal("Concluída", response.Status);
    }

    [Fact]
    public async Task CreateAsync_TrimsTitleAndDescription()
    {
        var service = _builder.Build();

        var response = await service.CreateAsync(
            TaskServiceBuilder.ValidCreateRequest(title: "  Título  ", description: "   "));

        Assert.Equal("Título", response.Title);
        Assert.Null(response.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithoutTitle_ThrowsValidationException(string? title)
    {
        var service = _builder.Build();
        var request = TaskServiceBuilder.ValidCreateRequest() with { Title = title };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
        Assert.Empty(_builder.Context.Tasks);
    }

    [Fact]
    public async Task CreateAsync_WithTitleTooLong_ThrowsValidationException()
    {
        var service = _builder.Build();
        var request = TaskServiceBuilder.ValidCreateRequest(title: new string('a', 201));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task CreateAsync_WithDescriptionTooLong_ThrowsValidationException()
    {
        var service = _builder.Build();
        var request = TaskServiceBuilder.ValidCreateRequest(description: new string('a', 2001));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Description));
    }

    [Theory]
    [InlineData("Cancelada")]
    [InlineData("Done")]
    [InlineData("1")]
    public async Task CreateAsync_WithInvalidStatus_ThrowsValidationException(string status)
    {
        var service = _builder.Build();
        var request = TaskServiceBuilder.ValidCreateRequest(status: status);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Status));
        Assert.Empty(_builder.Context.Tasks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateAsync_WithoutStatus_ThrowsValidationException(string? status)
    {
        var service = _builder.Build();
        var request = TaskServiceBuilder.ValidCreateRequest() with { Status = status };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Status));
    }

    [Theory]
    [InlineData("pendente", "Pendente")]
    [InlineData("EM PROGRESSO", "Em progresso")]
    [InlineData("EmProgresso", "Em progresso")]
    [InlineData("concluida", "Concluída")]
    public async Task CreateAsync_AcceptsStatusIgnoringCaseAndAccents(string status, string expected)
    {
        var service = _builder.Build();

        var response = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(status: status));

        Assert.Equal(expected, response.Status);
    }
}
