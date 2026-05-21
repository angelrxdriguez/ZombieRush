using Godot;
using ZombieRush.Features.Zombies;
using System.Collections.Generic;

namespace ZombieRush.Features.Weapons;

public partial class BulletProjectile : Node2D
{
    private const uint WorldCollisionMask = 1;
    private const uint ZombieCollisionMask = 4;
    private static readonly Dictionary<string, Texture2D?> TextureCache = new();
    private static readonly HashSet<string> MissingTextureWarnings = new();

    private Vector2 _direction = Vector2.Right;
    private int _damage;
    private float _speed;
    private float _maxRange;
    private float _knockbackStrength;
    private float _distanceTraveled;
    private Texture2D? _visualTexture;

    public void Initialize(
        Vector2 direction,
        int damage,
        float speed,
        float maxRange,
        float knockbackStrength = 0.0f,
        string? visualTexturePath = null)
    {
        _direction = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
        _damage = Mathf.Max(0, damage);
        _speed = Mathf.Max(1.0f, speed);
        _maxRange = Mathf.Max(1.0f, maxRange);
        _knockbackStrength = Mathf.Max(0.0f, knockbackStrength);
        _visualTexture = string.IsNullOrWhiteSpace(visualTexturePath)
            ? null
            : LoadVisualTexture(visualTexturePath);
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
        if (_visualTexture is not null)
        {
            var halfSize = new Vector2(_visualTexture.GetWidth() * 0.5f, _visualTexture.GetHeight() * 0.5f);
            DrawTexture(_visualTexture, -halfSize);
            return;
        }

        DrawLine(new Vector2(-9.0f, 0.0f), new Vector2(9.0f, 0.0f), new Color(1.0f, 0.88f, 0.34f, 1.0f), 3.0f, true);
        DrawCircle(new Vector2(10.0f, 0.0f), 2.4f, new Color(1.0f, 0.97f, 0.76f, 1.0f));
    }

    private static Texture2D? LoadVisualTexture(string texturePath)
    {
        if (TextureCache.TryGetValue(texturePath, out var cachedTexture))
        {
            return cachedTexture;
        }

        var loadedTexture = ResourceLoader.Load<Texture2D>(texturePath);
        TextureCache[texturePath] = loadedTexture;

        if (loadedTexture is null && MissingTextureWarnings.Add(texturePath))
        {
            GD.PushWarning($"No se pudo cargar la textura del proyectil: {texturePath}");
        }

        return loadedTexture;
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
