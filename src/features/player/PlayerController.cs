using System;
using Godot;

namespace ZombieRush.Features.Player;

public partial class PlayerController : CharacterBody2D
{
    private const string MoveLeftAction = "move_left";
    private const string MoveRightAction = "move_right";
    private const string MoveUpAction = "move_up";
    private const string MoveDownAction = "move_down";
    private const string DashAction = "dash";

    [Export(PropertyHint.Range, "120,720,10")]
    public float MoveSpeed { get; set; } = 320.0f;

    [Export(PropertyHint.Range, "240,1200,10")]
    public float DashSpeed { get; set; } = 760.0f;

    [Export(PropertyHint.Range, "0.05,0.5,0.01")]
    public float DashDurationSeconds { get; set; } = 0.12f;

    [Export(PropertyHint.Range, "1,500,1")]
    public int MaxHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0;

    public Vector2 FacingDirection { get; private set; } = Vector2.Up;

    public event Action<int, int>? HealthChanged;

    public event Action? HealthDepleted;

    private bool _isDashing;
    private double _dashTimeRemaining;
    private Vector2 _dashVelocity = Vector2.Zero;

    public override void _Ready()
    {
        MaxHealth = Math.Max(1, MaxHealth);
        CurrentHealth = MaxHealth;
        EmitHealthChanged();
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.LengthSquared() <= 0.0001f)
        {
            return;
        }

        FacingDirection = direction.Normalized();
        Rotation = FacingDirection.Angle() + Mathf.Pi / 2.0f;
    }

    public int ApplyDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return 0;
        }

        var nextHealth = Math.Max(0, CurrentHealth - amount);
        var appliedDamage = CurrentHealth - nextHealth;
        if (appliedDamage == 0)
        {
            return 0;
        }

        CurrentHealth = nextHealth;
        EmitHealthChanged();

        if (CurrentHealth == 0)
        {
            HealthDepleted?.Invoke();
        }

        return appliedDamage;
    }

    public int Heal(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var nextHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        var healedAmount = nextHealth - CurrentHealth;
        if (healedAmount == 0)
        {
            return 0;
        }

        CurrentHealth = nextHealth;
        EmitHealthChanged();
        return healedAmount;
    }

    public void RestoreFullHealth()
    {
        if (CurrentHealth == MaxHealth)
        {
            return;
        }

        CurrentHealth = MaxHealth;
        EmitHealthChanged();
    }

    public bool StartDash(Vector2 direction, float speed, float durationSeconds)
    {
        if (!IsAlive || _isDashing || speed <= 0.0f || durationSeconds <= 0.0f)
        {
            return false;
        }

        if (direction.LengthSquared() <= 0.0001f)
        {
            direction = FacingDirection;
        }

        _dashVelocity = direction.Normalized() * speed;
        _dashTimeRemaining = durationSeconds;
        _isDashing = true;
        return true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var inputDirection = GetMovementInputDirection();
        SetFacingDirection(GetGlobalMousePosition() - GlobalPosition);

        if (!_isDashing && Input.IsActionJustPressed(DashAction))
        {
            var dashDirection = inputDirection.LengthSquared() > 0.001f
                ? inputDirection
                : FacingDirection;

            StartDash(dashDirection, DashSpeed, DashDurationSeconds);
        }

        if (_isDashing)
        {
            Velocity = _dashVelocity;
            MoveAndSlide();

            _dashTimeRemaining -= delta;
            if (_dashTimeRemaining <= 0.0)
            {
                _isDashing = false;
                _dashVelocity = Vector2.Zero;
            }

            return;
        }

        Velocity = inputDirection * MoveSpeed;
        MoveAndSlide();
    }

    private static Vector2 GetMovementInputDirection()
    {
        return Input.GetVector(
            MoveLeftAction,
            MoveRightAction,
            MoveUpAction,
            MoveDownAction);
    }

    private void EmitHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
