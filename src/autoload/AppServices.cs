using System;

using Godot;

using ZombieRush.Core.Config;
using ZombieRush.Persistence;

namespace ZombieRush.Autoload;

public partial class AppServices : Node
{
    public static AppServices? Instance { get; private set; }

    public SaveDatabase Database { get; private set; } = default!;

    public PlayerProfileRepository PlayerProfiles { get; private set; } = default!;

    public override void _EnterTree()
    {
        if (Instance is not null && Instance != this)
        {
            throw new InvalidOperationException("AppServices is already initialized.");
        }

        Instance = this;
    }

    public override void _Ready()
    {
        var databasePath = ProjectSettings.GlobalizePath($"user://{GameConstants.SaveFileName}");

        Database = new SaveDatabase(databasePath, new SqliteMigrationCatalog());
        Database.Initialize();

        PlayerProfiles = new PlayerProfileRepository(Database.Connection);

        var profile = PlayerProfiles.GetOrCreate();

        GD.Print($"SQLite ready at: {databasePath}");
        GD.Print($"Current highest wave: {profile.HighestWave}");
    }

    public override void _ExitTree()
    {
        Database?.Dispose();

        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }
}
