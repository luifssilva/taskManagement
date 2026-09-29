using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Configurations;

internal sealed class TaskItemStatusDefinitionConfiguration : IEntityTypeConfiguration<TaskItemStatusDefinition>
{
    public void Configure(EntityTypeBuilder<TaskItemStatusDefinition> builder)
    {
        // The key is the enum value itself (stored as int), so the enum and the table stay aligned.
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Description)
            .IsRequired()
            .HasMaxLength(TaskItemStatusDefinition.DescriptionMaxLength);

        builder.HasData(TaskItemStatusDefinition.All);
    }
}
