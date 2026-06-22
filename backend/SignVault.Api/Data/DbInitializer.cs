using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace SignVault.Api.Data;

/// <summary>Brings the database schema up to date at startup, per provider.</summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Database.IsSqlite())
        {
            // Local dev and tests: build the schema directly from the model. SQLite databases
            // here are disposable, so they don't need a migration history.
            db.Database.EnsureCreated();
            return;
        }

        // Production (PostgreSQL): apply real migrations so the schema can evolve safely.
        // If this database predates migrations — it was first created with EnsureCreated and
        // therefore has tables but no __EFMigrationsHistory — baseline it: record the initial
        // migration as already applied instead of trying to recreate existing tables. The
        // EnsureCreated schema and the InitialCreate migration come from the same model, so the
        // baseline is safe and loses no data.
        var creator = db.GetService<IRelationalDatabaseCreator>();
        var history = db.GetService<IHistoryRepository>();
        if (creator.Exists() && creator.HasTables() && !history.Exists())
        {
            db.Database.ExecuteSqlRaw(history.GetCreateScript());
            var initialMigration = db.Database.GetMigrations().First();
            db.Database.ExecuteSqlRaw(
                history.GetInsertScript(new HistoryRow(initialMigration, ProductInfo.GetVersion())));
        }

        db.Database.Migrate();
    }
}
