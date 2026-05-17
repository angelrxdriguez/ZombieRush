using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Data.Sqlite;

namespace ZombieRush.Persistence;

public sealed class SaveDatabase : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteMigrationCatalog _migrationCatalog;

    private SqliteConnection? _connection;

    public SaveDatabase(string databasePath, SqliteMigrationCatalog migrationCatalog)
    {
        _databasePath = databasePath;
        _migrationCatalog = migrationCatalog;
    }

    public SqliteConnection Connection =>
        _connection ?? throw new InvalidOperationException("Database has not been initialized yet.");

    public void Initialize()
    {
        EnsureDatabaseDirectory();

        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        EnsureMigrationTable();
        ApplyPendingMigrations();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    private void EnsureDatabaseDirectory()
    {
        var directory = Path.GetDirectoryName(_databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void EnsureMigrationTable()
    {
        using var command = Connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version TEXT PRIMARY KEY,
                applied_utc TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private void ApplyPendingMigrations()
    {
        var appliedVersions = GetAppliedVersions();

        foreach (var migration in _migrationCatalog.GetAll().Where(migration => !appliedVersions.Contains(migration.Version)))
        {
            using var transaction = Connection.BeginTransaction();

            using (var migrationCommand = Connection.CreateCommand())
            {
                migrationCommand.Transaction = transaction;
                migrationCommand.CommandText = migration.Sql;
                migrationCommand.ExecuteNonQuery();
            }

            using (var recordCommand = Connection.CreateCommand())
            {
                recordCommand.Transaction = transaction;
                recordCommand.CommandText = """
                    INSERT INTO schema_migrations (version, applied_utc)
                    VALUES ($version, $appliedUtc);
                    """;
                recordCommand.Parameters.AddWithValue("$version", migration.Version);
                recordCommand.Parameters.AddWithValue("$appliedUtc", DateTime.UtcNow.ToString("O"));
                recordCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    private HashSet<string> GetAppliedVersions()
    {
        var versions = new HashSet<string>(StringComparer.Ordinal);

        using var command = Connection.CreateCommand();
        command.CommandText = "SELECT version FROM schema_migrations;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            versions.Add(reader.GetString(0));
        }

        return versions;
    }
}
