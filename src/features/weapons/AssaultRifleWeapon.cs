using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class AssaultRifleWeapon : PlayerWeapon
{
    private const string ProjectileTexturePath = "res://assets/GunsPack/Bullets/PistolAmmoSmall.png";

    [Export(PropertyHint.Range, "1,200,1")]
    public int Damage { get; set; } = 25;

    [Export(PropertyHint.Range, "1,120,1")]
    public int MagazineSize { get; set; } = 30;

    [Export(PropertyHint.Range, "0,600,1")]
    public int InitialReserveBullets { get; set; } = 90;

    [Export(PropertyHint.Range, "0.1,5.0,0.05")]
    public float ReloadDurationSeconds { get; set; } = 2.0f;

    [Export(PropertyHint.Range, "240,2400,20")]
    public float ProjectileSpeed { get; set; } = 1700.0f;

    [Export(PropertyHint.Range, "120,2400,20")]
    public float ProjectileRange { get; set; } = 1100.0f;

    [Export(PropertyHint.Range, "0,96,2")]
    public float BulletSpawnDistance { get; set; } = 38.0f;

    [Export(PropertyHint.Range, "0,30,0.5")]
    public float MaxSpreadAngleDegrees { get; set; } = 9.0f;

    [Export(PropertyHint.Range, "1,30,1")]
    public int ShotsUntilMaxSpread { get; set; } = 6;

    [Export(PropertyHint.Range, "0.05,2.0,0.05")]
    public float SpreadResetSeconds { get; set; } = 0.35f;

    private int _bulletsInMagazine;
    private int _reserveBullets;
    private double _reloadTimeRemaining;
    private double _timeSinceLastShot;
    private int _consecutiveShots;
    private bool _hasInitializedAmmo;
    private readonly RandomNumberGenerator _spreadRng = new();

    public AssaultRifleWeapon()
    {
        WeaponId = "ak47";
        DisplayName = "AK47";
        HudIconPath = "res://assets/GunsPack/Guns/AK47.png";
        PurchasePrice = 2200;
        CooldownSeconds = 0.10f;
        _spreadRng.Randomize();
    }

    public override void _Ready()
    {
        InitializeAmmo();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_consecutiveShots > 0)
        {
            _timeSinceLastShot += delta;
            if (_timeSinceLastShot >= SpreadResetSeconds)
            {
                _consecutiveShots = 0;
            }
        }

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
            _bulletsInMagazine >= MagazineSize ||
            _reserveBullets <= 0)
        {
            return false;
        }

        _reloadTimeRemaining = ReloadDurationSeconds;
        _consecutiveShots = 0;
        EmitStateChanged();
        return true;
    }

    public override bool IsReloading => _reloadTimeRemaining > 0.0;

    public override float GetReloadProgress01()
    {
        if (_reloadTimeRemaining <= 0.0)
        {
            return 0.0f;
        }

        var safeReloadDuration = Mathf.Max(0.01f, ReloadDurationSeconds);
        var progress = 1.0f - (float)(_reloadTimeRemaining / safeReloadDuration);
        return Mathf.Clamp(progress, 0.0f, 1.0f);
    }

    public override string GetHudDetail()
    {
        InitializeAmmo();

        return _reloadTimeRemaining > 0.0
            ? "Recargando"
            : $"{_bulletsInMagazine}/{_reserveBullets}";
    }

    public override bool SupportsAmmoRestock => true;

    public override bool SupportsAutomaticFire => true;

    public override bool IsAmmoFull()
    {
        InitializeAmmo();
        return _reloadTimeRemaining <= 0.0 &&
            _bulletsInMagazine >= MagazineSize &&
            _reserveBullets >= InitialReserveBullets;
    }

    public override bool RestockAmmo()
    {
        InitializeAmmo();

        var ammoChanged = _reloadTimeRemaining > 0.0 ||
            _bulletsInMagazine != MagazineSize ||
            _reserveBullets != InitialReserveBullets;

        _reloadTimeRemaining = 0.0;
        _bulletsInMagazine = MagazineSize;
        _reserveBullets = InitialReserveBullets;

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

        if (_bulletsInMagazine <= 0)
        {
            TryReload(owner);
            return false;
        }

        var projectileParent = owner.GetTree()?.CurrentScene ?? owner.GetParent();
        if (projectileParent is null)
        {
            return false;
        }

        var firingDirection = ApplySpread(direction);

        var bullet = new BulletProjectile();
        projectileParent.AddChild(bullet);
        bullet.GlobalPosition = owner.GlobalPosition + (firingDirection * BulletSpawnDistance);
        bullet.Initialize(
            firingDirection,
            Damage,
            ProjectileSpeed,
            ProjectileRange,
            visualTexturePath: ProjectileTexturePath);

        _bulletsInMagazine--;
        _consecutiveShots++;
        _timeSinceLastShot = 0.0;
        EmitStateChanged();
        return true;
    }

    private Vector2 ApplySpread(Vector2 direction)
    {
        if (_consecutiveShots <= 0 || MaxSpreadAngleDegrees <= 0.0f)
        {
            return direction;
        }

        var rampSteps = Mathf.Max(1, ShotsUntilMaxSpread);
        var rampFactor = Mathf.Clamp((float)_consecutiveShots / rampSteps, 0.0f, 1.0f);
        var spreadRadians = Mathf.DegToRad(MaxSpreadAngleDegrees) * rampFactor;
        var randomOffset = _spreadRng.RandfRange(-spreadRadians, spreadRadians);
        return direction.Rotated(randomOffset);
    }

    private void InitializeAmmo()
    {
        if (_hasInitializedAmmo)
        {
            return;
        }

        MagazineSize = Math.Max(1, MagazineSize);
        InitialReserveBullets = Math.Max(0, InitialReserveBullets);
        _bulletsInMagazine = MagazineSize;
        _reserveBullets = InitialReserveBullets;
        _hasInitializedAmmo = true;
    }

    private void CompleteReload()
    {
        var missingBullets = MagazineSize - _bulletsInMagazine;
        if (missingBullets <= 0 || _reserveBullets <= 0)
        {
            EmitStateChanged();
            return;
        }

        var bulletsToLoad = Math.Min(missingBullets, _reserveBullets);
        _bulletsInMagazine += bulletsToLoad;
        _reserveBullets -= bulletsToLoad;
        EmitStateChanged();
    }
}
