using Godot;

// Drawing helpers shared by every script that draws with code (characters, houses, trees, world).
// These are C# extension methods: "this CanvasItem item" lets us call them as if they were
// built into Godot, e.g. DrawEllipse(...) inside any Node2D's _Draw().
public static class DrawUtils
{
    // Dark color used for cartoon outlines.
    public static readonly Color Outline = new(0.12f, 0.09f, 0.1f);

    // A filled ellipse (Godot only has DrawCircle), built from a polygon of 20 points.
    public static void DrawEllipse(this CanvasItem item, Vector2 center, Vector2 radius, Color color)
    {
        var points = new Vector2[20];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length;
            points[i] = center + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        item.DrawColoredPolygon(points, color);
    }

    // A circle with a dark outline around it.
    public static void DrawOutlinedCircle(this CanvasItem item, Vector2 center, float radius, Color color, float outline = 1.5f)
    {
        item.DrawCircle(center, radius + outline, Outline);
        item.DrawCircle(center, radius, color);
    }

    // A thick line with rounded ends (an arm, a leg, a torso), with a dark outline.
    public static void DrawLimb(this CanvasItem item, Vector2 from, Vector2 to, float width, Color color, float outline = 1.5f)
    {
        float outer = width / 2 + outline;
        item.DrawLine(from, to, Outline, width + outline * 2);
        item.DrawCircle(from, outer, Outline);
        item.DrawCircle(to, outer, Outline);

        item.DrawLine(from, to, color, width);
        item.DrawCircle(from, width / 2, color);
        item.DrawCircle(to, width / 2, color);
    }

    // A filled rectangle with a dark border.
    public static void DrawOutlinedRect(this CanvasItem item, Rect2 rect, Color color, float outline = 2f)
    {
        item.DrawRect(rect, color);
        item.DrawRect(rect, Outline, filled: false, width: outline);
    }
}
