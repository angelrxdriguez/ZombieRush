using Godot;
using ZombieRush.Features.Zombies;

namespace ZombieRush.Features.Weapons;

public partial class BulletProjectile : Node2D
{
    private const uint WorldCollisionMask = 1;
    private const uint ZombieCollisionMask = 4;

    private Vector2 _direction = Vector2.Right;
    private int _damage;
    private float _speed;
    private float _maxRange;
    private float _knockbackStrength;
    private float _distanceTraveled;

    public void Initialize(
        Vector2 direction,
        int damage,
        float speed,
        float maxRange,
        float knockbackStrength = 0.0f)
    {
        _direction = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
        _damage = Mathf.Max(0, damage);
        _speed = Mathf.Max(1.0f, speed);
        _maxRange = Mathf.Max(1.0f, maxRange);
        _knockbackStrength = Mathf.Max(0.0f, knockbackStrength);
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
        var hit = CastBulletRay(startPosition, endPosition);

        if (hit.Count > 0)
        {
            GlobalPosition = hit["position"].AsVector2();

            if (hit["collider"].AsGodotObject() is ZombieController zombie)
            {
                zombie.ApplyDamage(_damage, GlobalPosition);
                if (_knockbackStrength > 0.0f)
                {
                    zombie.ApplyKnockback(_direction * _knockbackStrength);
                }
            }

            QueueFree();
            return;
        }

        GlobalPosition = endPosition;
        _distanceTraveled += stepDistance;

        if (_distanceTraveled >= _maxRange)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawLine(new Vector2(-9.0f, 0.0f), new Vector2(9.0f, 0.0f), new Color(1.0f, 0.88f, 0.34f, 1.0f), 3.0f, true);
        DrawCircle(new Vector2(10.0f, 0.0f), 2.4f, new Color(1.0f, 0.97f, 0.76f, 1.0f));
    }

    private Godot.Collections.Dictionary CastBulletRay(Vector2 startPosition, Vector2 endPosition)
    {
        var world = GetWorld2D();
        if (world is null)
        {
            return [];
        }

        var query = PhysicsRayQueryParameters2D.Create(startPosition, endPosition);
        query.CollisionMask = WorldCollisionMask | ZombieCollisionMask;
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;

        return world.DirectSpaceState.IntersectRay(query);
    }
}
