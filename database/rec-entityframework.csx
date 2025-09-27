#r "nuget: Lestaly.General, 0.104.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.4"
#r "nuget: Microsoft.EntityFrameworkCore.Sqlite, 9.0.9"
#nullable enable
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Lestaly;
using Npgsql;

// This script must be run with `--isolated-load-context` to avoid assembly race problems.


// Utility class for creating DB settings
static partial class DbPreferense
{
    // Create DB connection settings for SQLite
    static public DbContextOptions CreateSqliteOptions() => createSqliteOptions(builder =>
    {
        builder.DataSource = ThisSource.RelativeFile("storage.db").FullName;
        builder.ForeignKeys = true;
        builder.Pooling = false;
    });

    // Create DB connection settings for PostgreSQL
    static public DbContextOptions CreatePostgresOptions() => createPostgresOptions(builder =>
    {
        builder.Host = "my-host";
        builder.Port = 5432;
        builder.Database = "storage-db";
        builder.Username = "storage-db";
        builder.Password = "storage-db";
    });
}

return await Paved.ProceedAsync(async () =>
{
    // Create DB connection option
    var dbOptions = DbPreferense.CreateSqliteOptions();
    //var dbOptions = DbPreferense.CreatePostgresOptions();

    // Create DB context
    using var db = new DriveSpaceDbContext(dbOptions);

    // Ensure database create
    await db.Database.EnsureCreatedAsync();

    // Retrieve all existing drive entities in DB
    var manageDrives = await db.Drives.ToListAsync();

    // Check if the most recent record is stored in the DB.
    var recentTime = DateTime.UtcNow.AddHours(-1);
    var recentRecorded = await db.Inspects.AnyAsync(i => recentTime < i.Time);
    if (!recentRecorded)
    {
        // Prepare information about the inspection.
        var inspect = new Inspect();
        inspect.Time = DateTime.UtcNow;
        inspect.Spaces = new List<Space>();
        db.Inspects.Add(inspect);

        // Obtain drive information for the system.
        foreach (var info in DriveInfo.GetDrives())
        {
            // Skip non-fixed drives.
            if (info.DriveType != DriveType.Fixed) continue;

            // Find an existing entity that represents the target drive.
            var drive = manageDrives.FirstOrDefault(d => d.Name == info.Name);
            if (drive == null)
            {
                // If it is an unmanaged drive, create a new drive entity and add management.
                drive = new Drive();
                drive.Name = info.Name;
                db.Drives.Add(drive);
                manageDrives.Add(drive);
            }

            // Create an entity for free space information.
            var space = new Space();
            space.Drive = drive;
            space.Inspect = inspect;
            space.TotalSize = info.TotalSize;
            space.TotalFree = info.TotalFreeSpace;
            space.AvailableFree = info.AvailableFreeSpace;

            // Add free space information to the inspection information.
            inspect.Spaces.Add(space);
        }

        // Save changes to DB.
        await db.SaveChangesAsync();
    }

    // Query information on the last three inspections.
    var histInspects = db.Inspects
        .Include(i => i.Spaces).ThenInclude(s => s.Drive)
        .OrderByDescending(i => i.Time)
        .Take(3)
        .AsAsyncEnumerable();

    // Displays acquired information.
    await foreach (var hist in histInspects)
    {
        Console.WriteLine($"{hist.Time.ToLocalTime():yyyy/MM/dd HH:mm:ss}");
        foreach (var space in hist.Spaces)
        {
            var freeRatio = (double)space.AvailableFree / space.TotalSize;
            Console.WriteLine($"  {space.Drive.Name}: {space.AvailableFree.ToHumanize(),5} / {space.TotalSize.ToHumanize(),5} ({freeRatio,6:P})");
        }
    }
});


// DB context to manage drive space
class DriveSpaceDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Drive> Drives { get; set; } = default!;
    public DbSet<Inspect> Inspects { get; set; } = default!;
    public DbSet<Space> Spaces { get; set; } = default!;
}

// entity for drive
class Drive
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
}

// entity for inspection
class Inspect
{
    public long Id { get; set; }
    public DateTime Time { get; set; }

    public IList<Space> Spaces { get; set; } = default!;
}

// entity for free space
class Space
{
    public long Id { get; set; }
    public long InspectId { get; set; }
    public long DriveId { get; set; }
    public long TotalSize { get; set; }
    public long TotalFree { get; set; }
    public long AvailableFree { get; set; }

    [ForeignKey(nameof(InspectId))]
    public Inspect Inspect { get; set; } = default!;
    [ForeignKey(nameof(DriveId))]
    public Drive Drive { get; set; } = default!;
}

// Utility class for creating DB settings(Regular processing part)
static partial class DbPreferense
{
    static DbContextOptions createSqliteOptions(Action<SqliteConnectionStringBuilder> building)
    {
        var connBuilder = new SqliteConnectionStringBuilder();
        building(connBuilder);

        var optBuilder = new DbContextOptionsBuilder<DriveSpaceDbContext>();
        optBuilder.UseSqlite(connBuilder.ConnectionString);

        return optBuilder.Options;
    }

    static DbContextOptions createPostgresOptions(Action<NpgsqlConnectionStringBuilder> building)
    {
        var connBuilder = new NpgsqlConnectionStringBuilder();
        building(connBuilder);

        var optBuilder = new DbContextOptionsBuilder<DriveSpaceDbContext>();
        optBuilder.UseNpgsql(connBuilder.ConnectionString);

        return optBuilder.Options;
    }
}
