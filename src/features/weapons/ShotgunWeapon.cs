using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class ShotgunWeapon : PlayerWeapon
{
    [Export(PropertyHint.Range, "1,200,1")]
    public int DamagePerPellet { get; set; } = 22;

    [Export(PropertyHint.Range, "2,16,1")]
    public int PelletCount { get; set; } = 8;

    [Export(PropertyHint.Range, "4,80,1")]
    public float SpreadAngleDegrees { get; set; } = 34.0f;

    [Export(PropertyHint.Range, "0,96,2")]
    public float PelletSpawnDistance { get; set; } = 34.0f;

    [Export(PropertyHint.Range, "120,2400,20")]
    public float ProjectileSpeed { get; set; } = 1080.0f;

    [Export(PropertyHint.Range, "60,1200,10")]
    public float ProjectileRange { get; set; } = 340.0f;

    [Export(PropertyHint.Range, "0,900,10")]
    public float KnockbackStrength { get; set; } = 130.0f;

    [Export(PropertyHint.Range, "1,24,1")]
    public int MagazineSize { get; set; } = 6;

    [Export(PropertyHint.Range, "0,120,1")]
    public int InitialReserveShells { get; set; } = 24;

    [Export(PropertyHint.Range, "0.1,5.0,0.05")]
    public float ReloadDurationSeconds { get; set; } = 1.55f;

    private int _shellsInMagazine;
    private int _reserveShells;
    private double _reloadTimeRemaining;
    private bool _hasInitializedAmmo;

    public ShotgunWeapon()
    {
        WeaponId = "shotgun_12g";
        DisplayName = "Escopeta 12G";
        PurchasePrice = 2800;
        CooldownSeconds = 0.92f;
    }

    public override void _Ready()
    {
        InitializeAmmo();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_reloadTimeRemaining <= 0.0)
        {
            return;
        }

        _reloadTimeRemaining = Math.Max(0.0, _reloadTimeRemaining - delta);
        if (_reloadTimeRemaining <= 0.0)
        {
            CompleteReload();
        }
    }

    public override bool TryReload(PlayerController owner)
    {
        InitializeAmmo();

        if (!owner.IsAlive ||
            _reloadTimeRemaining > 0.0 ||
            _shellsInMagazine >= MagazineSize ||
            _reserveShells <= 0)
        {
            return false;
        }

        _reloadTimeRemaining = ReloadDurationSeconds;
        EmitStateChanged();
        return true;
    }

    public override string GetHudDetail()
    {
        InitializeAmmo();

        return _reloadTimeRemaining > 0.0
            ? "Recargando"
            : $"{_shellsInMagazine}/{_reserveShells}";
    }

    public override bool SupportsAmmoRestock => true;

    public override bool IsAmmoFull()
    {
        InitializeAmmo();
        return _reloadTimeRemaining <= 0.0 &&
            _shellsInMagazine >= MagazineSize &&
            _reserveShells >= InitialReserveShells;
    }

    public override bool RestockAmmo()
    {
        InitializeAmmo();

        var ammoChanged = _reloadTimeRemaining > 0.0 ||
            _shellsInMagazine != MagazineSize ||
            _reserveShells != InitialReserveShells;

        _reloadTimeRemaining = 0.0;
        _shellsInMagazine = MagazineSize;
        _reserveShells = InitialReserveShells;

        if (ammoChanged)
        {
            EmitStateChanged();
        }

        return ammoChanged;
    }

    protected override bool Use(PlayerController owner, Vector2 direction)
    {
        InitializeAmmo();

        if (_reloadTimeRemaining > 0.0)
        {
            return false;
        }

        if (_shellsInMagazine <= 0)
        {
            TryReload(owner);
            return false;
        }

        var projectileParent = owner.GetTree()?.CurrentScene ?? owner.GetParent();
        if (projectileParent is null)
        {
            return false;
        }

        var pellets = Math.Max(2, PelletCount);
        var spreadRadians = Mathf.DegToRad(Mathf.Max(0.0f, SpreadAngleDegrees));
        var halfSpread = spreadRadians * 0.5f;
        var step = pellets > 1 ? spreadRadians / (pellets - 1) : 0.0f;

        for (var pelletIndex = 0; pelletIndex < pellets; pelletIndex++)
        {
            var angleOffset = -halfSpread + (step * pelletIndex);
            var pelletDirection = direction.Rotated(angleOffset).Normalized();

            var pellet = new BulletProjectile();
            projectileParent.AddChild(pellet);
            pellet.GlobalPosition = owner.GlobalPosition + (pelletDirection * PelletSpawnDistance);
            pellet.Initialize(
                pelletDirection,
                DamagePerPellet,
                ProjectileSpeed,
                ProjectileRange,
                KnockbackStrength);
        }

        _shellsInMagazine--;
        EmitStateChanged();
        return true;
    }

    private void InitializeAmmo()
    {
        if (_hasInitializedAmmo)
        {
            return;
        }

        MagazineSize = Math.Max(1, MagazineSize);
        InitialReserveShells = Math.Max(0, InitialReserveShells);
        _shellsInMagazine = MagazineSize;
        _reserveShells = InitialReserveShells;
        _hasInitializedAmmo = true;
    }

    private void CompleteReload()
    {
        var missingShells = MagazineSize - _shellsInMagazine;
        if (missingShells <= 0 || _reserveShells <= 0)
        {
            EmitStateChanged();
            return;
        }

        var shellsToLoad = Math.Min(missingShells, _reserveShells);
        _shellsInMagazine += shellsToLoad;
        _reserveShells -= shellsToLoad;
        EmitStateChanged();
    }
}
