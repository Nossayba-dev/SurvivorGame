using Godot;

// A village house drawn with code. It blocks characters and bullets.
//
// (0, 0) is the bottom-center of the walls (where the door is). The walls and roof go up.
// Only the walls have collision: a character can stand "above" the house, where it is hidden
// behind the roof thanks to Y-sorting. This fakes a 3D view in a 2D game.
public partial class House : StaticBody2D
{
    [Export] public float Width = 140f;
    [Export] public float WallHeight = 70f;
    [Export] public float RoofHeight = 55f;
    [Export] public Color WallColor = new(0.93f, 0.86f, 0.72f);
    [Export] public Color RoofColor = new(0.72f, 0.28f, 0.24f);

    public override void _Ready()
    {
        // Created in code because each house has its own size. If every house shared one shape
        // Resource from the .tscn, resizing it for one house would resize it for all of them.
        var collision = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(Width, WallHeight) },
            Position = new Vector2(0, -WallHeight / 2),
        };
        AddChild(collision);
    }

    public override void _Draw()
    {
        float left = -Width / 2;
        float top = -WallHeight;

        // Shadow on the ground, offset to the bottom-right.
        DrawRect(new Rect2(left + 8, -4, Width, 12), new Color(0, 0, 0, 0.22f));

        DrawChimney(top);

        // Walls, with a stone foundation at the bottom.
        this.DrawOutlinedRect(new Rect2(left, top, Width, WallHeight), WallColor);
        this.DrawOutlinedRect(new Rect2(left, -9, Width, 9), new Color(0.6f, 0.58f, 0.55f));

        // Wooden beams on the corners.
        Color beam = new(0.45f, 0.3f, 0.2f);
        DrawRect(new Rect2(left + 2, top, 6, WallHeight - 9), beam);
        DrawRect(new Rect2(-left - 8, top, 6, WallHeight - 9), beam);

        // Door with a knob.
        this.DrawOutlinedRect(new Rect2(-11, -38, 22, 38), new Color(0.5f, 0.32f, 0.2f));
        DrawLine(new Vector2(0, -38), new Vector2(0, 0), new Color(0.4f, 0.25f, 0.15f), 1.5f);
        DrawCircle(new Vector2(6, -18), 1.8f, new Color(0.95f, 0.8f, 0.3f));

        // Two windows, placed at a quarter of the width.
        DrawWindow(new Vector2(-Width / 4 - 4, top + 20));
        DrawWindow(new Vector2(Width / 4 + 4, top + 20));

        DrawRoof(left, top);
    }

    private void DrawChimney(float top)
    {
        // Drawn before the roof, so the roof covers its bottom part.
        var rect = new Rect2(Width / 4, top - RoofHeight - 6, 14, 30);
        this.DrawOutlinedRect(rect, new Color(0.55f, 0.35f, 0.3f));
    }

    private void DrawWindow(Vector2 center)
    {
        var rect = new Rect2(center - new Vector2(11, 9), new Vector2(22, 18));
        this.DrawOutlinedRect(rect, new Color(0.62f, 0.82f, 0.95f));
        // Window frame cross and a light reflection.
        DrawLine(center + new Vector2(0, -9), center + new Vector2(0, 9), DrawUtils.Outline, 1.5f);
        DrawLine(center + new Vector2(-11, 0), center + new Vector2(11, 0), DrawUtils.Outline, 1.5f);
        DrawLine(center + new Vector2(-8, -6), center + new Vector2(-4, -6), new Color(1, 1, 1, 0.8f), 1.5f);
    }

    private void DrawRoof(float left, float top)
    {
        // A trapezoid that overhangs the walls on both sides.
        float bottom = top + 6;
        float roofTop = top - RoofHeight;
        Vector2[] roof =
        {
            new(left - 10, bottom), new(-left + 10, bottom),
            new(-left - 14, roofTop), new(left + 14, roofTop),
        };
        DrawColoredPolygon(roof, RoofColor);

        // Rows of tiles: darker horizontal lines that follow the slope of the roof sides.
        for (float y = roofTop + 10; y < bottom; y += 10)
        {
            float t = (y - roofTop) / (bottom - roofTop);   // 0 at the top, 1 at the bottom
            float halfWidth = Mathf.Lerp(-left - 14, -left + 10, t);
            DrawLine(new Vector2(-halfWidth, y), new Vector2(halfWidth, y), RoofColor.Darkened(0.2f), 2f);
        }

        // Ridge at the top and outline all around (the first point is repeated to close the shape).
        DrawLine(roof[3], roof[2], RoofColor.Darkened(0.35f), 4f);
        DrawPolyline(new[] { roof[0], roof[1], roof[2], roof[3], roof[0] }, DrawUtils.Outline, 2f);
    }
}
