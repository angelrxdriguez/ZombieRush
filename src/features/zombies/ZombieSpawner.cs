using System;
using Godot;

namespace ZombieRush.Features.Zombies;

public partial class ZombieSpawner : Node
{
    private const string DefaultZombieScenePath = "res://scenes/zombies/zombie_basic.tscn";
    private const string DefaultIronZombieScenePath = "res://scenes/zombies/zombie_iron.tscn";
    private const string DefaultBruteZombieScenePath = "res://scenes/zombies/zombie_brute.tscn";

    [Export]
    public PackedScene? ZombieScene { get; set; }

    [Export]
    public PackedScene? IronZombieScene { get; set; }

    [Export]
    public PackedScene? BruteZombieScene { get; set; }

    [Export]
    public NodePath SpawnPointsPath { get; set; } = "../../CurrentMap/ZombieSpawns";

    [Export]
    public NodePath ZombieContainerPath { get; set; } = "../Zombies";

    [Export(PropertyHint.Range, "1,200,1")]
    public int InitialSpawnCount { get; set; } = 5;

    [Export(PropertyHint.Range, "0,200,1")]
    public int ZombiesAddedPerWave { get; set; } = 5;

    [Export(PropertyHint.Range, "0.05,3.0,0.05")]
    public float SpawnIntervalBetweenZombiesSeconds { get; set; } = 0.1f;

    [Export(PropertyHint.Range, "1,300,1")]
    public int BaseConcurrentZombies { get; set; } = 7;

    [Export(PropertyHint.Range, "0,100,1")]
    public int ConcurrentZombiesAddedPerWave { get; set; } = 2;

    [Export(PropertyHint.Range, "1,500,1")]
    public int MaxConcurrentZombies { get; set; } = 36;

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
    private int _remainingBruteToSpawnInWave;
    private int _aliveInWave;
    private int _zombiesInCurrentWave;
    private double _spawnCooldownRemaining;

    public override void _Ready()
    {
        ZombieScene ??= ResourceLoader.Load<PackedScene>(DefaultZombieScenePath);
        IronZombieScene ??= ResourceLoader.Load<PackedScene>(DefaultIronZombieScenePath);
        BruteZombieScene ??= ResourceLoader.Load<PackedScene>(DefaultBruteZombieScenePath);
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
            if (_spawnCooldownRemaining <= 0.0 && ShouldSpawnMoreZombies())
            {
                SpawnBatch();
                _spawnCooldownRemaining = Mathf.Max(0.05f, SpawnIntervalBetweenZombiesSeconds);
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
        var bruteZombiesThisWave = GetBruteZombiesForWave(_currentWave);
        var zombiesThisWave = basicZombiesThisWave + ironZombiesThisWave + bruteZombiesThisWave;

        _remainingBasicToSpawnInWave = basicZombiesThisWave;
        _remainingIronToSpawnInWave = ironZombiesThisWave;
        _remainingBruteToSpawnInWave = bruteZombiesThisWave;
        _remainingToSpawnInWave = zombiesThisWave;
        _zombiesInCurrentWave = zombiesThisWave;
        _aliveInWave = 0;
        _spawnCooldownRemaining = 0.0f;

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

        var aliveDeficit = Math.Max(1, GetTargetAliveForCurrentWave() - _aliveInWave);
        var spawnCount = Math.Min(1, _remainingToSpawnInWave);
        spawnCount = Math.Min(spawnCount, aliveDeficit);

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

    private bool ShouldSpawnMoreZombies()
    {
        return _aliveInWave < GetTargetAliveForCurrentWave();
    }

    private int GetTargetAliveForCurrentWave()
    {
        var baseTarget = Math.Max(1, BaseConcurrentZombies);
        var waveIncrement = Math.Max(0, ConcurrentZombiesAddedPerWave) * Math.Max(0, _currentWave - 1);
        var cappedTarget = Math.Min(Math.Max(1, MaxConcurrentZombies), baseTarget + waveIncrement);
        var waveTotal = Math.Max(1, _zombiesInCurrentWave);
        return Math.Min(cappedTarget, waveTotal);
    }

    private PackedScene SelectSceneForNextZombie()
    {
        if (_remainingBruteToSpawnInWave > 0)
        {
            _remainingBruteToSpawnInWave--;
            return BruteZombieScene ?? ZombieScene!;
        }

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

        if (_remainingBruteToSpawnInWave > 0)
        {
            _remainingBruteToSpawnInWave--;
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

    private static int GetBruteZombiesForWave(int waveNumber)
    {
        if (waveNumber <= 0 || (waveNumber % 5) != 0)
        {
            return 0;
        }

        return 1;
    }
}
