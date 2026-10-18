using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimer.Domain.Entities;

namespace PomodoroTimer.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.FocusDurationMinutes)
            .IsRequired()
            .HasDefaultValue(25);

        builder.Property(s => s.ShortBreakMinutes)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(s => s.LongBreakMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(s => s.CyclesBeforeLongBreak)
            .IsRequired()
            .HasDefaultValue(4);

        builder.Property(s => s.SoundEnabled)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
