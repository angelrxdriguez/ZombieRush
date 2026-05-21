using System;
using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Weapons;

public partial class AssaultRifleWeapon : PlayerWeapon
{
    private const string ProjectileTexturePath = "res://assets/GunsPack/Bullets/PistolAmmoSmall.png";
    private const string FireSoundPath = "res://assets/audio/sfx/Guns Sound Effects/AK47/Sound_of_an_AK47_being_fired. (1).wav";
    private static AudioStream? _cachedFireSound;
    private static bool _hasAttemptedFireSoundLoad;

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

    [Export(PropertyHint.Range, "-40.0,12.0,0.5")]
    public float FireSoundVolumeDb { get; set; } = -6.0f;

    [Export(PropertyHint.Range, "0.5,2.0,0.05")]
    public float FireSoundPitchScale { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0.0,0.5,0.01")]
    public float FireSoundPitchVariation { get; set; } = 0.08f;

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

        PlayFireSound(projectileParent, owner.GlobalPosition);

        _bulletsInMagazine--;
        _consecutiveShots++;
        _timeSinceLastShot = 0.0;
        EmitStateChanged();
        return true;
    }

    private void PlayFireSound(Node parent, Vector2 position)
    {
        _ = position;

        var stream = LoadFireSound();
        if (stream is null)
        {
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = FireSoundVolumeDb,
            PitchScale = FireSoundPitchScale + _spreadRng.RandfRange(-FireSoundPitchVariation, FireSoundPitchVariation),
        };

        parent.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    private static AudioStream? LoadFireSound()
    {
        if (_hasAttemptedFireSoundLoad)
        {
            return _cachedFireSound;
        }

        _hasAttemptedFireSoundLoad = true;

        if (ResourceLoader.Exists(FireSoundPath))
        {
            _cachedFireSound = ResourceLoader.Load<AudioStream>(FireSoundPath);
            if (_cachedFireSound is not null)
            {
                return _cachedFireSound;
            }
        }

        _cachedFireSound = LoadWavFromDisk(FireSoundPath);
        if (_cachedFireSound is null)
        {
            GD.PushWarning($"No se pudo cargar el sonido del AK47: {FireSoundPath}");
        }

        return _cachedFireSound;
    }

    private static AudioStreamWav? LoadWavFromDisk(string resourcePath)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(resourcePath);
        if (bytes is null || bytes.Length < 44)
        {
            return null;
        }

        if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F' ||
            bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E')
        {
            return null;
        }

        var offset = 12;
        var fmtChunkOffset = -1;
        var dataChunkOffset = -1;
        var dataChunkSize = 0;

        while (offset + 8 <= bytes.Length)
        {
            var chunkId = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = BitConverter.ToInt32(bytes, offset + 4);
            offset += 8;

            if (chunkId == "fmt ")
            {
                fmtChunkOffset = offset;
            }
            else if (chunkId == "data")
            {
                dataChunkOffset = offset;
                dataChunkSize = chunkSize;
                break;
            }

            offset += chunkSize;
            if ((chunkSize & 1) != 0)
            {
                offset++;
            }
        }

        if (fmtChunkOffset < 0 || dataChunkOffset < 0 || dataChunkSize <= 0 ||
            dataChunkOffset + dataChunkSize > bytes.Length)
        {
            return null;
        }

        var audioFormat = BitConverter.ToInt16(bytes, fmtChunkOffset);
        var numChannels = BitConverter.ToInt16(bytes, fmtChunkOffset + 2);
        var sampleRate = BitConverter.ToInt32(bytes, fmtChunkOffset + 4);
        var bitsPerSample = BitConverter.ToInt16(bytes, fmtChunkOffset + 14);

        if (audioFormat != 1 || (bitsPerSample != 8 && bitsPerSample != 16) || numChannels is < 1 or > 2)
        {
            return null;
        }

        var pcmData = new byte[dataChunkSize];
        Array.Copy(bytes, dataChunkOffset, pcmData, 0, dataChunkSize);

        return new AudioStreamWav
        {
            Data = pcmData,
            Format = bitsPerSample == 16
                ? AudioStreamWav.FormatEnum.Format16Bits
                : AudioStreamWav.FormatEnum.Format8Bits,
            MixRate = sampleRate,
            Stereo = numChannels == 2,
        };
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
