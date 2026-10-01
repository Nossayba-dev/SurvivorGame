using Godot;

// The play area. Draws the ground, a grid and a border.
// The walls that block movement are child nodes in World.tscn.
public partial class World : Node2D
{
    // The map is centered on (0, 0).
    // If you change it, also update the walls in World.tscn and the camera limits in Main.tscn.
    [Export] public Vector2 MapSize = new Vector2(2400, 1600);
    [Export] public int CellSize = 64;
    [Export] public Color GroundColor = new Color(0.16f, 0.2f, 0.16f);
    [Export] public Color GridColor = new Color(0.22f, 0.27f, 0.22f);
    [Export] public Color BorderColor = new Color(0.55f, 0.45f, 0.3f);

    // Called by Godot when this node needs to be drawn (once at start, and again after QueueRedraw()).
    public override void _Draw()
    {
        Rect2 area = new Rect2(-MapSize / 2, MapSize);

        DrawRect(area, GroundColor);

        for (float x = area.Position.X; x <= area.End.X; x += CellSize)
            DrawLine(new Vector2(x, area.Position.Y), new Vector2(x, area.End.Y), GridColor, 2);

        for (float y = area.Position.Y; y <= area.End.Y; y += CellSize)
            DrawLine(new Vector2(area.Position.X, y), new Vector2(area.End.X, y), GridColor, 2);

        DrawRect(area, BorderColor, filled: false, width: 8);
    }
}
