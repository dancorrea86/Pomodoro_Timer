using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimer.Domain.Entities;

namespace PomodoroTimer.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.EstimatedPomodoros)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(t => t.CompletedPomodoros)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasMany(t => t.Sessions)
            .WithOne(s => s.Task)
            .HasForeignKey(s => s.TaskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
