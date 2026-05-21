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

    public event Action<int>? Damaged;

    private bool _isDashing;
    private double _dashTimeRemaining;
    private Vector2 _dashVelocity = Vector2.Zero;

    private CanvasItem? _visual;
    private Camera2D? _camera;
    private Tween? _damageFlashTween;
    private Tween? _cameraShakeTween;
    private Vector2 _cameraBaseOffset = Vector2.Zero;
    private readonly RandomNumberGenerator _feedbackRandom = new();

    public override void _Ready()
    {
        MaxHealth = Math.Max(1, MaxHealth);
        CurrentHealth = MaxHealth;
        EmitHealthChanged();

        _visual = GetNodeOrNull<CanvasItem>("Visual");
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (_camera is not null)
        {
            _cameraBaseOffset = _camera.Offset;
        }
        _feedbackRandom.Randomize();
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

        Damaged?.Invoke(appliedDamage);
        PlayDamageVisualFlash();
        PlayCameraShake();

        if (CurrentHealth == 0)
        {
            HealthDepleted?.Invoke();
        }

        return appliedDamage;
    }

    private void PlayDamageVisualFlash()
    {
        if (_visual is null || !IsInstanceValid(_visual))
        {
            return;
        }

        _damageFlashTween?.Kill();
        _visual.Modulate = new Color(1.8f, 0.45f, 0.45f, 1.0f);
        _damageFlashTween = CreateTween();
        _damageFlashTween.TweenProperty(_visual, "modulate", Colors.White, 0.32f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    private void PlayCameraShake()
    {
        if (_camera is null || !IsInstanceValid(_camera))
        {
            return;
        }

        _cameraShakeTween?.Kill();

        const float intensity = 8.0f;
        const int steps = 6;
        const float stepDuration = 0.035f;

        _cameraShakeTween = CreateTween();
        for (var i = 0; i < steps; i++)
        {
            var falloff = 1.0f - ((float)i / steps);
            var offset = new Vector2(
                _feedbackRandom.RandfRange(-intensity, intensity) * falloff,
                _feedbackRandom.RandfRange(-intensity, intensity) * falloff);
            _cameraShakeTween.TweenProperty(_camera, "offset", _cameraBaseOffset + offset, stepDuration);
        }
        _cameraShakeTween.TweenProperty(_camera, "offset", _cameraBaseOffset, stepDuration);
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
