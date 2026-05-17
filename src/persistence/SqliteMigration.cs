namespace ZombieRush.Persistence;

public sealed record SqliteMigration(string Version, string Sql);
