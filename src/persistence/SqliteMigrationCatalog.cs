using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Godot;

namespace ZombieRush.Persistence;

public sealed class SqliteMigrationCatalog
{
    public IReadOnlyList<SqliteMigration> GetAll() =>
    [
        new SqliteMigration(
            "001_initial",
            LoadMigrationSql("res://data/database/migrations/001_initial.sql")
        )
    ];

    private static string LoadMigrationSql(string resourcePath)
    {
        var sql = Godot.FileAccess.GetFileAsString(resourcePath);

        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException($"Migration file could not be loaded: {resourcePath}");
        }

        return sql!;
    }
}
