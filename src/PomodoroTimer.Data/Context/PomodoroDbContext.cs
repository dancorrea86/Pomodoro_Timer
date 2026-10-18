using Microsoft.EntityFrameworkCore;
using PomodoroTimer.Domain.Entities;

namespace PomodoroTimer.Data.Context;

public class PomodoroDbContext : DbContext
{
    public PomodoroDbContext(DbContextOptions<PomodoroDbContext> options)
        : base(options)
    {
    }

    public PomodoroDbContext()
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<PomodoroSession> PomodoroSessions => Set<PomodoroSession>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDirectory = Path.Combine(appDataFolder, "PomodoroTimer");
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            var dbPath = Path.Combine(dbDirectory, "pomodoro.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PomodoroDbContext).Assembly);
    }
}
