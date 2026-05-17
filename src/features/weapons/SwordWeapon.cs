using System.Collections.Generic;
using Godot;
using ZombieRush.Features.Player;
using ZombieRush.Features.Zombies;

namespace ZombieRush.Features.Weapons;

public partial class SwordWeapon : PlayerWeapon
{
    [Export(PropertyHint.Range, "1,200,1")]
    public int Damage { get; set; } = 25;

    [Export(PropertyHint.Range, "40,180,4")]
    public float AttackRange { get; set; } = 92.0f;

    [Export(PropertyHint.Range, "20,180,5")]
    public float AttackArcDegrees { get; set; } = 105.0f;

    [Export(PropertyHint.Range, "0.05,0.5,0.01")]
    public float SwingDurationSeconds { get; set; } = 0.22f;

    private readonly HashSet<ulong> _damagedZombieIds = [];
    private PlayerController? _activeOwner;
    private Vector2 _attackDirection = Vector2.Up;
    private float _currentSwingAngle;
    private double _attackElapsed;
    private double _attackTimeRemaining;

    public SwordWeapon()
    {
        WeaponId = "starter_sword";
        DisplayName = "Espada";
        CooldownSeconds = 0.45f;
    }

    public override void _Ready()
    {
        Visible = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_activeOwner is null || !IsInstanceValid(_activeOwner))
        {
            EndAttack();
            return;
        }

        if (_attackTimeRemaining <= 0.0)
        {
            EndAttack();
            return;
        }

        _attackElapsed += delta;
        _attackDirection = _activeOwner.FacingDirection;
        UpdateSwingVisual();
        ApplyDamageToZombiesInAttackArc(_activeOwner);
        _attackTimeRemaining -= delta;
    }

    protected override bool Use(PlayerController owner, Vector2 direction)
    {
        owner.SetFacingDirection(direction);
        _activeOwner = owner;
        _attackDirection = direction;
        _attackElapsed = 0.0;
        _attackTimeRemaining = SwingDurationSeconds;
        _damagedZombieIds.Clear();
        Visible = true;
        UpdateSwingVisual();
        ApplyDamageToZombiesInAttackArc(owner);
        return true;
    }

    public override void _Draw()
    {
        if (!Visible)
        {
            return;
        }

        var bladeDirection = Vector2.Up.Rotated(_currentSwingAngle);
        var bladeStart = bladeDirection * 28.0f;
        var bladeEnd = bladeDirection * AttackRange;
        var bladeSide = new Vector2(-bladeDirection.Y, bladeDirection.X);
        var arcStart = Vector2.Up.Rotated(-GetHalfArcRadians()).Angle();
        var arcEnd = Vector2.Up.Rotated(GetHalfArcRadians()).Angle();

        DrawArc(Vector2.Zero, AttackRange, arcStart, arcEnd, 24, new Color(0.95f, 0.9f, 0.72f, 0.35f), 3.0f, true);
        DrawLine(bladeStart, bladeEnd, new Color(0.9f, 0.92f, 0.95f, 1.0f), 7.0f, true);
        DrawLine(bladeStart, bladeEnd, new Color(0.45f, 0.58f, 0.7f, 1.0f), 2.0f, true);
        DrawLine(bladeStart - (bladeSide * 12.0f), bladeStart + (bladeSide * 12.0f), new Color(0.82f, 0.56f, 0.2f, 1.0f), 5.0f, true);
        DrawCircle(bladeEnd, 3.0f, new Color(1.0f, 0.96f, 0.78f, 1.0f));
    }

    private void ApplyDamageToZombiesInAttackArc(PlayerController owner)
    {
        var tree = GetTree();
        if (tree is null)
        {
            return;
        }

        var rangeSquared = AttackRange * AttackRange;
        var halfArcRadians = GetHalfArcRadians();

        foreach (var node in tree.GetNodesInGroup(ZombieController.ZombieGroup))
        {
            if (node is not ZombieController zombie || !zombie.IsAlive)
            {
                continue;
            }

            var zombieId = zombie.GetInstanceId();
            if (_damagedZombieIds.Contains(zombieId))
            {
                continue;
            }

            var zombieOffset = zombie.GlobalPosition - owner.GlobalPosition;
            if (zombieOffset.LengthSquared() > rangeSquared || zombieOffset.LengthSquared() <= 0.0001f)
            {
                continue;
            }

            var zombieDirection = zombieOffset.Normalized();
            if (Mathf.Abs(_attackDirection.AngleTo(zombieDirection)) > halfArcRadians)
            {
                continue;
            }

            zombie.ApplyDamage(Damage);
            _damagedZombieIds.Add(zombieId);
        }
    }

    private void UpdateSwingVisual()
    {
        var duration = Mathf.Max(0.01f, SwingDurationSeconds);
        var progress = Mathf.Clamp((float)(_attackElapsed / duration), 0.0f, 1.0f);
        _currentSwingAngle = Mathf.Lerp(-GetHalfArcRadians(), GetHalfArcRadians(), progress);
        QueueRedraw();
    }

    private float GetHalfArcRadians()
    {
        return Mathf.DegToRad(AttackArcDegrees * 0.5f);
    }

    private void EndAttack()
    {
        _activeOwner = null;
        _attackDirection = Vector2.Up;
        _currentSwingAngle = 0.0f;
        _attackElapsed = 0.0;
        _attackTimeRemaining = 0.0;
        _damagedZombieIds.Clear();
        Visible = false;
        QueueRedraw();
    }
}
