using Godot;

namespace ZombieRush.Features.Zombies;

public partial class ZombieSpawner : Node
{
    private const string DefaultZombieScenePath = "res://scenes/zombies/zombie_basic.tscn";

    [Export]
    public PackedScene? ZombieScene { get; set; }

    [Export]
    public NodePath SpawnPointsPath { get; set; } = "../../CurrentMap/ZombieSpawns";

    [Export]
    public NodePath ZombieContainerPath { get; set; } = "../Zombies";

    [Export(PropertyHint.Range, "1,200,1")]
    public int InitialSpawnCount { get; set; } = 5;

    private bool _hasSpawnedInitialWave;

    public override void _Ready()
    {
        ZombieScene ??= ResourceLoader.Load<PackedScene>(DefaultZombieScenePath);
    }

    public void SpawnInitialWave(Node2D target)
    {
        if (_hasSpawnedInitialWave)
        {
            return;
        }

        if (ZombieScene is null)
        {
            GD.PushWarning("ZombieSpawner no tiene escena de zombie asignada.");
            return;
        }

        var spawnPointsRoot = GetNodeOrNull<Node>(SpawnPointsPath);
        var zombieContainer = GetNodeOrNull<Node>(ZombieContainerPath);
        if (spawnPointsRoot is null || zombieContainer is null)
        {
            GD.PushWarning("ZombieSpawner no encontro puntos de spawn o contenedor.");
            return;
        }

        var spawnPoints = GetSpawnPoints(spawnPointsRoot);
        if (spawnPoints.Count == 0)
        {
            GD.PushWarning("ZombieSpawner no encontro Marker2D en ZombieSpawns.");
            return;
        }

        for (var i = 0; i < InitialSpawnCount; i++)
        {
            var spawnPoint = spawnPoints[i % spawnPoints.Count];
            var zombie = ZombieScene.Instantiate<ZombieController>();
            zombie.GlobalPosition = spawnPoint.GlobalPosition;
            zombie.SetTarget(target);
            zombieContainer.AddChild(zombie);
        }

        _hasSpawnedInitialWave = true;
    }

    private static Godot.Collections.Array<Marker2D> GetSpawnPoints(Node spawnPointsRoot)
    {
        var points = new Godot.Collections.Array<Marker2D>();

        foreach (var child in spawnPointsRoot.GetChildren())
        {
            if (child is Marker2D marker)
            {
                points.Add(marker);
            }
        }

        return points;
    }
}
