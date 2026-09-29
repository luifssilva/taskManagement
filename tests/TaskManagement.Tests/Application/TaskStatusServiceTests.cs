using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

public class TaskStatusServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsSeededDomainTable()
    {
        var builder = new TaskServiceBuilder();
        var service = new TaskStatusService(new TaskStatusRepository(builder.Context));

        var statuses = await service.ListAsync();

        Assert.Equal(
            [
                (1, TaskItemStatus.Pendente, "Pendente"),
                (2, TaskItemStatus.EmProgresso, "Em progresso"),
                (3, TaskItemStatus.Concluida, "Concluída")
            ],
            statuses.Select(s => (s.Id, s.Name, s.Description)));
    }
}
