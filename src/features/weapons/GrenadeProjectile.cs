using System;
using System.Collections.Generic;
using Godot;
using ZombieRush.Features.Vfx;
using ZombieRush.Features.Zombies;

namespace ZombieRush.Features.Weapons;

public partial class GrenadeProjectile : Node2D
{
    private const string DefaultExplosionScenePath = "res://scenes/vfx/grenade_explosion_fx.tscn";

    [Export(PropertyHint.Range, "0.2,2.0,0.05")]
    public float FlightDurationSeconds { get; set; } = 0.55f;

    [Export(PropertyHint.Range, "0.05,1.5,0.05")]
    public float FuseDurationSeconds { get; set; } = 0.35f;

    [Export(PropertyHint.Range, "0,160,2")]
    public float ArcPeakHeight { get; set; } = 38.0f;

    [Export(PropertyHint.Range, "60,1200,10")]
    public float MaxThrowDistance { get; set; } = 520.0f;

    [Export(PropertyHint.Range, "1,200,1")]
    public int ExplosionDamage { get; set; } = 100;

    [Export(PropertyHint.Range, "40,400,4")]
    public float ExplosionRadius { get; set; } = 150.0f;

    [Export(PropertyHint.Range, "0,1200,10")]
    public float ExplosionKnockback { get; set; } = 540.0f;

    [Export(PropertyHint.Range, "4,32,1")]
    public float SpinRevolutions { get; set; } = 4.0f;

    [Export]
    public PackedScene? ExplosionScene { get; set; }

    private Vector2 _startPosition;
    private Vector2 _landingPosition;
    private float _flightElapsed;
    private float _fuseElapsed;
    private bool _hasLanded;
    private bool _hasExploded;
    private float _baseRotation;

    public void Initialize(Vector2 startPosition, Vector2 targetPosition)
    {
        _startPosition = startPosition;
        var offset = targetPosition - startPosition;
        var distance = offset.Length();
        if (distance > MaxThrowDistance)
        {
            offset = offset.Normalized() * MaxThrowDistance;
        }
        _landingPosition = startPosition + offset;
        GlobalPosition = startPosition;
        _baseRotation = (float)GD.RandRange(0.0, Mathf.Tau);
    }

    public override void _Ready()
    {
        ZIndex = 12;
        ExplosionScene ??= ResourceLoader.Load<PackedScene>(DefaultExplosionScenePath);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_hasExploded)
        {
            return;
        }

        if (!_hasLanded)
        {
            TickFlight(delta);
            return;
        }

        TickFuse(delta);
    }

    private void TickFlight(double delta)
    {
        _flightElapsed += (float)delta;
        var duration = Mathf.Max(0.05f, FlightDurationSeconds);
        var progress = Mathf.Clamp(_flightElapsed / duration, 0.0f, 1.0f);

        var groundPosition = _startPosition.Lerp(_landingPosition, progress);
        var arcOffset = Mathf.Sin(progress * Mathf.Pi) * ArcPeakHeight;
        GlobalPosition = groundPosition + new Vector2(0.0f, -arcOffset);

        var spinAmount = Mathf.Tau * SpinRevolutions * progress;
        Rotation = _baseRotation + spinAmount;

        var arcScale = 1.0f + (0.35f * Mathf.Sin(progress * Mathf.Pi));
        Scale = new Vector2(arcScale, arcScale);

        QueueRedraw();

        if (progress >= 1.0f)
        {
            _hasLanded = true;
            GlobalPosition = _landingPosition;
            Scale = Vector2.One;
        }
    }

    private void TickFuse(double delta)
    {
        _fuseElapsed += (float)delta;
        QueueRedraw();

        if (_fuseElapsed >= Mathf.Max(0.05f, FuseDurationSeconds))
        {
            Detonate();
        }
    }

    public override void _Draw()
    {
        if (_hasExploded)
        {
            return;
        }

        var shadowOffset = _hasLanded ? Vector2.Zero : Vector2.Zero;
        DrawCircle(shadowOffset + new Vector2(2.0f, 3.0f), 10.0f, new Color(0.02f, 0.02f, 0.02f, 0.45f));

        var bodyColor = new Color(0.18f, 0.32f, 0.18f, 1.0f);
        var bodyHighlight = new Color(0.32f, 0.5f, 0.3f, 1.0f);
        DrawCircle(Vector2.Zero, 9.0f, bodyColor);
        DrawCircle(new Vector2(-2.0f, -2.0f), 5.0f, bodyHighlight);

        DrawLine(new Vector2(-6.0f, -4.0f), new Vector2(6.0f, -4.0f), new Color(0.1f, 0.16f, 0.1f, 1.0f), 1.6f, true);
        DrawLine(new Vector2(-6.0f, 0.0f), new Vector2(6.0f, 0.0f), new Color(0.1f, 0.16f, 0.1f, 1.0f), 1.6f, true);
        DrawLine(new Vector2(-6.0f, 4.0f), new Vector2(6.0f, 4.0f), new Color(0.1f, 0.16f, 0.1f, 1.0f), 1.6f, true);

        DrawRect(new Rect2(new Vector2(-2.0f, -12.0f), new Vector2(4.0f, 4.0f)), new Color(0.62f, 0.6f, 0.42f, 1.0f), true);
        DrawLine(new Vector2(2.0f, -10.0f), new Vector2(7.0f, -8.0f), new Color(0.82f, 0.78f, 0.42f, 1.0f), 2.0f, true);

        if (_hasLanded)
        {
            var fuseRatio = Mathf.Clamp(_fuseElapsed / Mathf.Max(0.05f, FuseDurationSeconds), 0.0f, 1.0f);
            var pulseFrequency = 8.0f + (12.0f * fuseRatio);
            var pulse = 0.5f + (0.5f * Mathf.Sin(_fuseElapsed * pulseFrequency * Mathf.Tau));
            var glowRadius = 3.0f + (2.5f * pulse);
            var glowColor = new Color(1.0f, 0.32f + (0.4f * pulse), 0.18f, 0.6f + (0.35f * pulse));
            DrawCircle(new Vector2(0.0f, -10.0f), glowRadius, glowColor);
        }
    }

    private void Detonate()
    {
        if (_hasExploded)
        {
            return;
        }

        _hasExploded = true;
        ApplyExplosionDamage();
        SpawnExplosionFx();
        QueueFree();
    }

    private void ApplyExplosionDamage()
    {
        var tree = GetTree();
        if (tree is null)
        {
            return;
        }

        var center = _landingPosition;
        var radiusSquared = ExplosionRadius * ExplosionRadius;
        var hitZombies = new List<ZombieController>();

        foreach (var node in tree.GetNodesInGroup(ZombieController.ZombieGroup))
        {
            if (node is not ZombieController zombie || !zombie.IsAlive)
            {
                continue;
            }

            var offset = zombie.GlobalPosition - center;
            if (offset.LengthSquared() > radiusSquared)
            {
                continue;
            }

            hitZombies.Add(zombie);
        }

        foreach (var zombie in hitZombies)
        {
            var offset = zombie.GlobalPosition - center;
            var distance = offset.Length();
            var falloff = 1.0f - Mathf.Clamp(distance / ExplosionRadius, 0.0f, 1.0f);
            zombie.ApplyDamage(ExplosionDamage, zombie.GlobalPosition);

            if (ExplosionKnockback > 0.0f && distance > 0.001f)
            {
                var pushDirection = offset / distance;
                zombie.ApplyKnockback(pushDirection * ExplosionKnockback * (0.4f + (0.6f * falloff)));
            }
        }
    }

    private void SpawnExplosionFx()
    {
        if (ExplosionScene?.Instantiate() is not Node2D explosion)
        {
            return;
        }

        var parent = GetTree()?.CurrentScene ?? GetParent();
        if (parent is null)
        {
            explosion.QueueFree();
            return;
        }

        parent.AddChild(explosion);
        explosion.GlobalPosition = _landingPosition;

        if (explosion is GrenadeExplosionFx fx)
        {
            fx.Configure(ExplosionRadius);
        }
    }
}
