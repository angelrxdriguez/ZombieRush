using Godot;
using ZombieRush.Features.Player;

namespace ZombieRush.Features.Zombies;

public partial class ZombieBlueOrbProjectile : Node2D
{
    private const uint WorldCollisionMask = 1;
    private const uint PlayerCollisionMask = 2;

    private Vector2 _direction = Vector2.Right;
    private int _damage;
    private float _speed;
    private float _maxRange;
    private float _distanceTraveled;
    private float _spinDirection = 1.0f;

    public override void _Ready()
    {
        _spinDirection = GD.Randf() >= 0.5f ? 1.0f : -1.0f;
    }

    public void Initialize(Vector2 direction, int damage, float speed, float maxRange)
    {
        _direction = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
        _damage = Mathf.Max(0, damage);
        _speed = Mathf.Max(1.0f, speed);
        _maxRange = Mathf.Max(1.0f, maxRange);
        Rotation = _direction.Angle();
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        var stepDistance = Mathf.Min(_speed * (float)delta, _maxRange - _distanceTraveled);
        if (stepDistance <= 0.0f)
        {
            QueueFree();
            return;
        }

        var startPosition = GlobalPosition;
        var endPosition = startPosition + (_direction * stepDistance);
        var hit = CastProjectileRay(startPosition, endPosition);

        if (hit.Count > 0)
        {
            GlobalPosition = hit["position"].AsVector2();

            if (hit["collider"].AsGodotObject() is PlayerController player)
            {
                player.ApplyDamage(_damage);
            }

            QueueFree();
            return;
        }

        GlobalPosition = endPosition;
        _distanceTraveled += stepDistance;
        Rotation += _spinDirection * 7.5f * (float)delta;

        if (_distanceTraveled >= _maxRange)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 11.0f, new Color(0.05f, 0.36f, 0.91f, 0.25f));
        DrawCircle(Vector2.Zero, 7.0f, new Color(0.13f, 0.64f, 1.0f, 0.9f));
        DrawCircle(new Vector2(2.2f, -1.6f), 2.3f, new Color(0.76f, 0.94f, 1.0f, 0.95f));
    }

    private Godot.Collections.Dictionary CastProjectileRay(Vector2 startPosition, Vector2 endPosition)
    {
        var world = GetWorld2D();
        if (world is null)
        {
            return [];
        }

        var query = PhysicsRayQueryParameters2D.Create(startPosition, endPosition);
        query.CollisionMask = WorldCollisionMask | PlayerCollisionMask;
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;

        return world.DirectSpaceState.IntersectRay(query);
    }
}
