using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(TaskItem.TitleMaxLength);

        builder.Property(t => t.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        // Status is stored as the enum's int value, a foreign key to the status domain table.
        builder.HasOne<TaskItemStatusDefinition>()
            .WithMany()
            .HasForeignKey(t => t.Status)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.CreatedAt).IsRequired();

        // The history is part of the task aggregate: loaded, saved and deleted with it.
        builder.OwnsMany(t => t.StatusHistory, history =>
        {
            history.WithOwner().HasForeignKey("TaskItemId");
            history.Property<int>("Id");
            history.HasKey("Id");

            history.HasOne<TaskItemStatusDefinition>()
                .WithMany()
                .HasForeignKey(h => h.FromStatus)
                .OnDelete(DeleteBehavior.Restrict);

            history.HasOne<TaskItemStatusDefinition>()
                .WithMany()
                .HasForeignKey(h => h.ToStatus)
                .OnDelete(DeleteBehavior.Restrict);

            history.Property(h => h.ChangedAt).IsRequired();
        });

        builder.Navigation(t => t.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
