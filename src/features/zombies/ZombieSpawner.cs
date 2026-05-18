using System;
using Godot;

namespace ZombieRush.Features.Zombies;

public partial class ZombieSpawner : Node
{
    private const string DefaultZombieScenePath = "res://scenes/zombies/zombie_basic.tscn";
    private const string DefaultIronZombieScenePath = "res://scenes/zombies/zombie_iron.tscn";

    [Export]
    public PackedScene? ZombieScene { get; set; }

    [Export]
    public PackedScene? IronZombieScene { get; set; }

    [Export]
    public NodePath SpawnPointsPath { get; set; } = "../../CurrentMap/ZombieSpawns";

    [Export]
    public NodePath ZombieContainerPath { get; set; } = "../Zombies";

    [Export(PropertyHint.Range, "1,200,1")]
    public int InitialSpawnCount { get; set; } = 5;

    [Export(PropertyHint.Range, "0,200,1")]
    public int ZombiesAddedPerWave { get; set; } = 5;

    [Export(PropertyHint.Range, "1,200,1")]
    public int SpawnBatchSize { get; set; } = 5;

    [Export(PropertyHint.Range, "0.5,60.0,0.5")]
    public float SpawnBatchIntervalSeconds { get; set; } = 10.0f;

    public event Action<ZombieController>? ZombieSpawned;

    public event Action<int, int>? WaveStarted;

    private readonly Godot.Collections.Array<Marker2D> _spawnPoints = [];
    private Node2D? _target;
    private Node? _zombieContainer;
    private bool _wavesStarted;
    private int _nextSpawnPointIndex;
    private int _currentWave;
    private int _remainingToSpawnInWave;
    private int _remainingBasicToSpawnInWave;
    private int _remainingIronToSpawnInWave;
    private int _aliveInWave;
    private double _spawnCooldownRemaining;

    public override void _Ready()
    {
        ZombieScene ??= ResourceLoader.Load<PackedScene>(DefaultZombieScenePath);
        IronZombieScene ??= ResourceLoader.Load<PackedScene>(DefaultIronZombieScenePath);
    }

    public override void _Process(double delta)
    {
        if (!_wavesStarted)
        {
            return;
        }

        if (_remainingToSpawnInWave > 0)
        {
            _spawnCooldownRemaining = Math.Max(0.0, _spawnCooldownRemaining - delta);
            if (_spawnCooldownRemaining <= 0.0)
            {
                SpawnBatch();
                _spawnCooldownRemaining = Math.Max(0.5f, SpawnBatchIntervalSeconds);
            }
        }

        if (_remainingToSpawnInWave == 0 && _aliveInWave == 0)
        {
            StartNextWave();
        }
    }

    public void SpawnInitialWave(Node2D target)
    {
        StartWaves(target);
    }

    public void StartWaves(Node2D target)
    {
        if (_wavesStarted)
        {
            return;
        }

        _target = target;

        if (!ResolveSpawnContext())
        {
            return;
        }

        _wavesStarted = true;
        StartNextWave();
    }

    private void StartNextWave()
    {
        _currentWave++;

        var baseCount = Math.Max(1, InitialSpawnCount);
        var addedPerWave = Math.Max(0, ZombiesAddedPerWave);
        var basicZombiesThisWave = baseCount + ((_currentWave - 1) * addedPerWave);
        var ironZombiesThisWave = GetIronZombiesForWave(_currentWave);
        var zombiesThisWave = basicZombiesThisWave + ironZombiesThisWave;

        _remainingBasicToSpawnInWave = basicZombiesThisWave;
        _remainingIronToSpawnInWave = ironZombiesThisWave;
        _remainingToSpawnInWave = zombiesThisWave;
        _aliveInWave = 0;
        _spawnCooldownRemaining = Math.Max(0.5f, SpawnBatchIntervalSeconds);

        WaveStarted?.Invoke(_currentWave, zombiesThisWave);
    }

    private void SpawnBatch()
    {
        if (_target is null || !IsInstanceValid(_target) ||
            _zombieContainer is null || !IsInstanceValid(_zombieContainer) ||
            ZombieScene is null ||
            _spawnPoints.Count == 0 ||
            _remainingToSpawnInWave <= 0)
        {
            return;
        }

        var batchSize = Math.Max(1, SpawnBatchSize);
        var spawnCount = Math.Min(batchSize, _remainingToSpawnInWave);

        for (var i = 0; i < spawnCount; i++)
        {
            var spawnPoint = _spawnPoints[_nextSpawnPointIndex % _spawnPoints.Count];
            _nextSpawnPointIndex++;

            var zombieScene = SelectSceneForNextZombie();
            var zombie = zombieScene.Instantiate<ZombieController>();
            zombie.GlobalPosition = spawnPoint.GlobalPosition;
            zombie.SetTarget(_target);
            zombie.HealthDepleted += OnWaveZombieDepleted;
            _zombieContainer.AddChild(zombie);

            _aliveInWave++;
            ZombieSpawned?.Invoke(zombie);
            _remainingToSpawnInWave--;
        }
    }

    private bool ResolveSpawnContext()
    {
        if (ZombieScene is null)
        {
            GD.PushWarning("ZombieSpawner no tiene escena de zombie asignada.");
            return false;
        }

        var spawnPointsRoot = GetNodeOrNull<Node>(SpawnPointsPath);
        _zombieContainer = GetNodeOrNull<Node>(ZombieContainerPath);

        if (spawnPointsRoot is null || _zombieContainer is null)
        {
            GD.PushWarning("ZombieSpawner no encontro puntos de spawn o contenedor.");
            return false;
        }

        _spawnPoints.Clear();

        foreach (var child in spawnPointsRoot.GetChildren())
        {
            if (child is Marker2D marker)
            {
                _spawnPoints.Add(marker);
            }
        }

        if (_spawnPoints.Count == 0)
        {
            GD.PushWarning("ZombieSpawner no encontro Marker2D en ZombieSpawns.");
            return false;
        }

        return true;
    }

    private void OnWaveZombieDepleted()
    {
        _aliveInWave = Math.Max(0, _aliveInWave - 1);
    }

    private PackedScene SelectSceneForNextZombie()
    {
        if (_remainingIronToSpawnInWave > 0 && IronZombieScene is not null)
        {
            _remainingIronToSpawnInWave--;
            return IronZombieScene;
        }

        if (_remainingBasicToSpawnInWave > 0)
        {
            _remainingBasicToSpawnInWave--;
            return ZombieScene!;
        }

        if (_remainingIronToSpawnInWave > 0)
        {
            _remainingIronToSpawnInWave--;
            return ZombieScene!;
        }

        return ZombieScene!;
    }

    private static int GetIronZombiesForWave(int waveNumber)
    {
        if (waveNumber <= 0 || (waveNumber % 2) != 0)
        {
            return 0;
        }

        return waveNumber / 2;
    }
}
