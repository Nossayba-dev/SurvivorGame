using Godot;

// A leafy tree drawn with code. Only the trunk blocks movement and bullets.
// (Not named "Tree": Godot already has a UI class called Tree.)
//
// (0, 0) is the base of the trunk, so Y-sorting puts characters in front of or behind it correctly.
public partial class TreeProp : StaticBody2D
{
    [Export] public float Size = 1f;
    [Export] public Color LeafColor = new(0.26f, 0.55f, 0.26f);

    public override void _Ready()
    {
        var collision = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 9 * Size },
            Position = new Vector2(0, -4 * Size),
        };
        AddChild(collision);
    }

    public override void _Draw()
    {
        float s = Size;
        this.DrawEllipse(new Vector2(4 * s, 0), new Vector2(24 * s, 8 * s), new Color(0, 0, 0, 0.22f));

        // Trunk.
        this.DrawOutlinedRect(new Rect2(-5 * s, -28 * s, 10 * s, 28 * s), new Color(0.45f, 0.3f, 0.18f));

        // Canopy: overlapping circles, darker at the bottom and lighter on top for a bit of volume.
        Color dark = LeafColor.Darkened(0.2f);
        Color light = LeafColor.Lightened(0.15f);
        Vector2[] centers = { new(0, -46), new(-15, -38), new(15, -38), new(-8, -56), new(9, -55) };
        float[] radii = { 21, 15, 15, 15, 14 };

        // Outline pass first (all circles slightly bigger and dark), then the fill pass on top.
        for (int i = 0; i < centers.Length; i++)
            DrawCircle(centers[i] * s, (radii[i] + 2) * s, DrawUtils.Outline);
        for (int i = 0; i < centers.Length; i++)
            DrawCircle(centers[i] * s, radii[i] * s, i < 3 ? dark : LeafColor);

        DrawCircle(new Vector2(-9, -60) * s, 6 * s, light);
        DrawCircle(new Vector2(5, -50) * s, 4 * s, light);
    }
}
