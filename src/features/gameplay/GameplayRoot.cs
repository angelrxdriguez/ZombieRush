using Godot;
using ZombieRush.Autoload;
using ZombieRush.Features.Economy;
using ZombieRush.Features.Zombies;

namespace ZombieRush.Features.Gameplay;

public partial class GameplayRoot : Node2D
{
    private const int MoneyPerZombieKill = 100;

    [Export]
    public NodePath PlayerPath { get; set; } = "Actors/Player";

    [Export]
    public NodePath PlayerSpawnPath { get; set; } = "CurrentMap/PlayerSpawn";

    [Export]
    public NodePath CameraPath { get; set; } = "Actors/Player/Camera2D";

    [Export]
    public NodePath CameraTopLeftPath { get; set; } = "CurrentMap/CameraBounds/TopLeft";

    [Export]
    public NodePath CameraBottomRightPath { get; set; } = "CurrentMap/CameraBounds/BottomRight";

    [Export(PropertyHint.Range, "0.25,4.0,0.05")]
    public float CameraZoom { get; set; } = 1.0f;

    [Export]
    public NodePath ZombieSpawnerPath { get; set; } = "Actors/ZombieSpawner";

    [Export]
    public NodePath MoneyWalletPath { get; set; } = "Economy/MoneyWallet";

    private MoneyWallet? _moneyWallet;

    public override void _Ready()
    {
        var player = GetNodeOrNull<Node2D>(PlayerPath);
        var playerSpawn = GetNodeOrNull<Marker2D>(PlayerSpawnPath);
        var playerCamera = GetNodeOrNull<Camera2D>(CameraPath);
        var cameraTopLeft = GetNodeOrNull<Marker2D>(CameraTopLeftPath);
        var cameraBottomRight = GetNodeOrNull<Marker2D>(CameraBottomRightPath);
        var zombieSpawner = GetNodeOrNull<ZombieSpawner>(ZombieSpawnerPath);
        _moneyWallet = GetNodeOrNull<MoneyWallet>(MoneyWalletPath);

        var profile = AppServices.Instance?.PlayerProfiles.GetOrCreate();
        if (_moneyWallet is not null && profile is not null)
        {
            _moneyWallet.SetBalance(profile.TotalCurrency);
        }

        if (player is null || playerSpawn is null)
        {
            GD.PushWarning("No se encontro el jugador o el punto PlayerSpawn.");
            return;
        }

        player.GlobalPosition = playerSpawn.GlobalPosition;

        if (playerCamera is null || cameraTopLeft is null || cameraBottomRight is null)
        {
            GD.PushWarning(
                $"No se pudo configurar la camara por falta de referencias. " +
                $"camera={playerCamera is not null}, " +
                $"topLeft={cameraTopLeft is not null}, " +
                $"bottomRight={cameraBottomRight is not null}");
            return;
        }

        ConfigureCamera(playerCamera, cameraTopLeft, cameraBottomRight, CameraZoom);

        if (zombieSpawner is not null)
        {
            zombieSpawner.ZombieSpawned += RegisterZombieKillReward;
            zombieSpawner.SpawnInitialWave(player);
        }
    }

    public override void _ExitTree()
    {
        var zombieSpawner = GetNodeOrNull<ZombieSpawner>(ZombieSpawnerPath);
        if (zombieSpawner is not null)
        {
            zombieSpawner.ZombieSpawned -= RegisterZombieKillReward;
        }
    }

    private void RegisterZombieKillReward(ZombieController zombie)
    {
        zombie.HealthDepleted += OnZombieKilled;
    }

    private void OnZombieKilled()
    {
        _moneyWallet?.AddMoney(MoneyPerZombieKill);
        AppServices.Instance?.PlayerProfiles.AddCurrency(MoneyPerZombieKill);
    }

    private static void ConfigureCamera(
        Camera2D camera,
        Marker2D topLeft,
        Marker2D bottomRight,
        float zoom)
    {
        var left = Mathf.Min(topLeft.GlobalPosition.X, bottomRight.GlobalPosition.X);
        var top = Mathf.Min(topLeft.GlobalPosition.Y, bottomRight.GlobalPosition.Y);
        var right = Mathf.Max(topLeft.GlobalPosition.X, bottomRight.GlobalPosition.X);
        var bottom = Mathf.Max(topLeft.GlobalPosition.Y, bottomRight.GlobalPosition.Y);

        camera.Zoom = new Vector2(zoom, zoom);
        camera.LimitLeft = Mathf.RoundToInt(left);
        camera.LimitTop = Mathf.RoundToInt(top);
        camera.LimitRight = Mathf.RoundToInt(right);
        camera.LimitBottom = Mathf.RoundToInt(bottom);
        camera.Enabled = true;
    }
}
