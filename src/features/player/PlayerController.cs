using System;
using Godot;

namespace ZombieRush.Features.Player;

public partial class PlayerController : CharacterBody2D
{
    private const string MoveLeftAction = "move_left";
    private const string MoveRightAction = "move_right";
    private const string MoveUpAction = "move_up";
    private const string MoveDownAction = "move_down";

    [Export(PropertyHint.Range, "120,720,10")]
    public float MoveSpeed { get; set; } = 320.0f;

    [Export(PropertyHint.Range, "1,500,1")]
    public int MaxHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int>? HealthChanged;

    public event Action? HealthDepleted;

    public override void _Ready()
    {
        MaxHealth = Math.Max(1, MaxHealth);
        CurrentHealth = MaxHealth;
        EmitHealthChanged();
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

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var inputDirection = Input.GetVector(
            MoveLeftAction,
            MoveRightAction,
            MoveUpAction,
            MoveDownAction);

        Velocity = inputDirection * MoveSpeed;
        MoveAndSlide();

        if (inputDirection.LengthSquared() > 0.001f)
        {
            Rotation = inputDirection.Angle() + Mathf.Pi / 2.0f;
        }
    }

    private void EmitHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
