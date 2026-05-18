using Godot;

namespace ZombieRush.Features.UI;

public partial class CrosshairCursor : Control
{
    [Export(PropertyHint.Range, "6,32,1")]
    public float Radius { get; set; } = 13.0f;

    [Export(PropertyHint.Range, "1,16,1")]
    public float Gap { get; set; } = 5.0f;

    [Export(PropertyHint.Range, "1,8,1")]
    public float LineWidth { get; set; } = 2.0f;

    [Export(PropertyHint.Range, "8,56,1")]
    public float ReloadRadius { get; set; } = 19.0f;

    [Export(PropertyHint.Range, "1,10,1")]
    public float ReloadLineWidth { get; set; } = 3.0f;

    private bool _isReloading;
    private float _reloadProgress01;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _ExitTree()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        Position = GetViewport().GetMousePosition();
        QueueRedraw();
    }

    public void SetReloadProgress(bool isReloading, float progress01)
    {
        _isReloading = isReloading;
        _reloadProgress01 = isReloading ? Mathf.Clamp(progress01, 0.0f, 1.0f) : 0.0f;
    }

    public override void _Draw()
    {
        var primary = new Color(0.95f, 0.96f, 0.9f, 0.95f);
        var accent = new Color(0.85f, 0.15f, 0.12f, 0.95f);
        var reloadTrack = new Color(0.9f, 0.92f, 0.84f, 0.28f);
        var reloadFill = new Color(0.98f, 0.54f, 0.22f, 0.98f);

        if (_isReloading)
        {
            var reloadStart = -Mathf.Pi / 2.0f;
            var reloadEnd = reloadStart + (_reloadProgress01 * Mathf.Pi * 2.0f);
            DrawArc(Vector2.Zero, ReloadRadius, 0.0f, Mathf.Pi * 2.0f, 48, reloadTrack, ReloadLineWidth, true);

            if (_reloadProgress01 > 0.001f)
            {
                DrawArc(Vector2.Zero, ReloadRadius, reloadStart, reloadEnd, 48, reloadFill, ReloadLineWidth, true);
            }
        }

        DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Pi * 2.0f, 32, primary, LineWidth, true);
        DrawLine(new Vector2(-Radius - 5.0f, 0.0f), new Vector2(-Gap, 0.0f), primary, LineWidth, true);
        DrawLine(new Vector2(Gap, 0.0f), new Vector2(Radius + 5.0f, 0.0f), primary, LineWidth, true);
        DrawLine(new Vector2(0.0f, -Radius - 5.0f), new Vector2(0.0f, -Gap), primary, LineWidth, true);
        DrawLine(new Vector2(0.0f, Gap), new Vector2(0.0f, Radius + 5.0f), primary, LineWidth, true);
        DrawCircle(Vector2.Zero, 2.5f, accent);
    }
}
