using System.Collections.Generic;
using Godot;

// The village. Draws the ground (grass, dirt roads, plaza, flowers) and creates the houses and trees.
// The walls around the map are child nodes in World.tscn.
public partial class World : Node2D
{
    // The map is centered on (0, 0).
    // If you change it, also update the walls in World.tscn and the camera limits in Main.tscn.
    [Export] public Vector2 MapSize = new Vector2(2400, 1600);
    [Export] public PackedScene HouseScene;
    [Export] public PackedScene TreeScene;
    // Houses and trees are added to this node. It is Y-sorted together with the characters,
    // so the player can walk in front of and behind them.
    [Export] public Node2D PropsContainer;
    // Same seed = same village every time. Change it to get different trees and flowers.
    [Export] public ulong Seed = 2026;

    private const float RoadHalfWidth = 44f;
    private const float PlazaRadius = 150f;

    private static readonly Color Grass = new(0.42f, 0.66f, 0.33f);
    private static readonly Color Dirt = new(0.76f, 0.63f, 0.44f);
    private static readonly Color Stone = new(0.68f, 0.65f, 0.6f);

    // Where each house stands: bottom-center of its walls, and its width.
    // Two rows on each side of the main road, leaving the vertical road free.
    private static readonly (Vector2 Position, float Width)[] HouseLayout =
    {
        (new(-950, -80), 150), (new(-700, -80), 130), (new(-430, -80), 150),
        (new(430, -80), 150), (new(700, -80), 130), (new(950, -80), 150),
        (new(-850, -420), 140), (new(-520, -440), 160), (new(-220, -400), 120),
        (new(250, -420), 130), (new(560, -440), 150), (new(900, -410), 140),
        (new(-950, 210), 150), (new(-680, 210), 130), (new(-400, 210), 150),
        (new(400, 210), 150), (new(680, 210), 130), (new(950, 210), 150),
        (new(-820, 560), 150), (new(-480, 580), 130), (new(-220, 550), 120),
        (new(230, 560), 130), (new(520, 580), 150), (new(860, 560), 140),
    };

    private static readonly Color[] RoofColors =
    {
        new(0.72f, 0.28f, 0.24f), new(0.3f, 0.42f, 0.62f), new(0.52f, 0.34f, 0.22f), new(0.34f, 0.5f, 0.32f),
    };

    private static readonly Color[] WallColors =
    {
        new(0.93f, 0.86f, 0.72f), new(0.86f, 0.86f, 0.84f), new(0.95f, 0.9f, 0.65f), new(0.9f, 0.78f, 0.7f),
    };

    // The space each house takes (walls + roof + a margin), used to keep trees away from houses.
    private readonly List<Rect2> _houseAreas = new();

    public override void _Ready()
    {
        SpawnHouses();
        SpawnTrees();
    }

    private void SpawnHouses()
    {
        for (int i = 0; i < HouseLayout.Length; i++)
        {
            var (position, width) = HouseLayout[i];   // tuple deconstruction

            var house = HouseScene.Instantiate<House>();
            house.Position = position;
            house.Width = width;
            house.RoofColor = RoofColors[i % RoofColors.Length];
            house.WallColor = WallColors[(i / 2) % WallColors.Length];
            PropsContainer.AddChild(house);

            float height = house.WallHeight + house.RoofHeight;
            _houseAreas.Add(new Rect2(position.X - width / 2 - 30, position.Y - height - 30, width + 60, height + 70));
        }
    }

    private void SpawnTrees()
    {
        var rng = new RandomNumberGenerator { Seed = Seed };
        Vector2 half = MapSize / 2;

        // A row of trees along every edge of the map: the forest border.
        for (float x = -half.X + 30; x < half.X; x += 60)
        {
            AddTree(new Vector2(x + rng.RandfRange(-10, 10), -half.Y + 40), rng);
            AddTree(new Vector2(x + rng.RandfRange(-10, 10), half.Y - 10), rng);
        }
        for (float y = -half.Y + 100; y < half.Y - 40; y += 60)
        {
            AddTree(new Vector2(-half.X + 25, y + rng.RandfRange(-10, 10)), rng);
            AddTree(new Vector2(half.X - 25, y + rng.RandfRange(-10, 10)), rng);
        }

        // Trees scattered in the gardens, away from roads, houses and each other.
        var placed = new List<Vector2>();
        int attempts = 0;
        while (placed.Count < 45 && attempts < 2000)
        {
            attempts++;
            var position = new Vector2(rng.RandfRange(-half.X + 100, half.X - 100), rng.RandfRange(-half.Y + 120, half.Y - 80));
            if (!IsGoodTreeSpot(position, placed))
                continue;

            placed.Add(position);
            AddTree(position, rng);
        }
    }

    private bool IsGoodTreeSpot(Vector2 position, List<Vector2> otherTrees)
    {
        if (IsOnRoad(position, margin: 40))
            return false;

        foreach (Rect2 area in _houseAreas)
            if (area.HasPoint(position))
                return false;

        foreach (Vector2 other in otherTrees)
            if (position.DistanceTo(other) < 80)
                return false;

        return true;
    }

    private void AddTree(Vector2 position, RandomNumberGenerator rng)
    {
        var tree = TreeScene.Instantiate<TreeProp>();
        tree.Position = position;
        tree.Size = rng.RandfRange(0.85f, 1.25f);
        tree.LeafColor = new Color(0.26f, 0.55f, 0.26f).Lightened(rng.RandfRange(-0.1f, 0.12f));
        PropsContainer.AddChild(tree);
    }

    private static bool IsOnRoad(Vector2 position, float margin = 0)
    {
        return Mathf.Abs(position.X) < RoadHalfWidth + margin
            || Mathf.Abs(position.Y) < RoadHalfWidth + margin
            || position.Length() < PlazaRadius + margin;
    }

    // Called by Godot once; the result is cached. Everything flat on the ground is drawn here.
    public override void _Draw()
    {
        // A fresh generator with the same seed: the drawing is identical every time.
        var rng = new RandomNumberGenerator { Seed = Seed };
        Vector2 half = MapSize / 2;
        var area = new Rect2(-half, MapSize);

        DrawRect(area, Grass);
        DrawGrassPatches(rng, half);
        DrawRoads(half);
        DrawPlaza();
        DrawPebbles(rng, half);
        DrawGrassTufts(rng, half);
        DrawFlowers(rng, half);
    }

    private void DrawGrassPatches(RandomNumberGenerator rng, Vector2 half)
    {
        for (int i = 0; i < 260; i++)
        {
            var center = new Vector2(rng.RandfRange(-half.X, half.X), rng.RandfRange(-half.Y, half.Y));
            Color color = rng.Randf() < 0.5f ? Grass.Darkened(0.06f) : Grass.Lightened(0.05f);
            this.DrawEllipse(center, new Vector2(rng.RandfRange(30, 80), rng.RandfRange(20, 50)), color);
        }
    }

    private void DrawRoads(Vector2 half)
    {
        Color edge = Dirt.Darkened(0.15f);
        // Darker, slightly bigger rectangles first: they become the road borders.
        DrawRect(new Rect2(-half.X, -RoadHalfWidth - 4, MapSize.X, RoadHalfWidth * 2 + 8), edge);
        DrawRect(new Rect2(-RoadHalfWidth - 4, -half.Y, RoadHalfWidth * 2 + 8, MapSize.Y), edge);
        DrawRect(new Rect2(-half.X, -RoadHalfWidth, MapSize.X, RoadHalfWidth * 2), Dirt);
        DrawRect(new Rect2(-RoadHalfWidth, -half.Y, RoadHalfWidth * 2, MapSize.Y), Dirt);
    }

    private void DrawPlaza()
    {
        DrawCircle(Vector2.Zero, PlazaRadius + 6, Stone.Darkened(0.25f));
        DrawCircle(Vector2.Zero, PlazaRadius, Stone);

        // Rings of cobblestones.
        for (float radius = 24; radius < PlazaRadius; radius += 22)
        {
            int count = (int)(Mathf.Tau * radius / 20);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Tau * i / count + radius;   // "+ radius" shifts each ring a bit
                var position = Vector2.FromAngle(angle) * radius;
                Color color = i % 2 == 0 ? Stone.Lightened(0.08f) : Stone.Darkened(0.08f);
                this.DrawEllipse(position, new Vector2(8, 6), color);
            }
        }

        // A mosaic in the middle.
        DrawCircle(Vector2.Zero, 16, Stone.Darkened(0.3f));
        DrawCircle(Vector2.Zero, 11, new Color(0.75f, 0.55f, 0.35f));
    }

    private void DrawPebbles(RandomNumberGenerator rng, Vector2 half)
    {
        for (int i = 0; i < 260; i++)
        {
            Vector2 position = rng.Randf() < 0.5f
                ? new Vector2(rng.RandfRange(-half.X, half.X), rng.RandfRange(-RoadHalfWidth + 4, RoadHalfWidth - 4))
                : new Vector2(rng.RandfRange(-RoadHalfWidth + 4, RoadHalfWidth - 4), rng.RandfRange(-half.Y, half.Y));
            if (position.Length() < PlazaRadius + 8)
                continue;
            this.DrawEllipse(position, new Vector2(rng.RandfRange(2, 4), rng.RandfRange(1.5f, 3)), Dirt.Darkened(rng.RandfRange(0.1f, 0.3f)));
        }
    }

    private void DrawGrassTufts(RandomNumberGenerator rng, Vector2 half)
    {
        Color tuft = Grass.Darkened(0.25f);
        for (int i = 0; i < 900; i++)
        {
            var position = new Vector2(rng.RandfRange(-half.X, half.X), rng.RandfRange(-half.Y, half.Y));
            if (IsOnRoad(position, margin: 6))
                continue;
            // Three little blades of grass.
            DrawLine(position, position + new Vector2(-3, -5), tuft, 1.5f);
            DrawLine(position, position + new Vector2(0, -7), tuft, 1.5f);
            DrawLine(position, position + new Vector2(3, -5), tuft, 1.5f);
        }
    }

    private void DrawFlowers(RandomNumberGenerator rng, Vector2 half)
    {
        Color[] petals = { Colors.White, new(1f, 0.85f, 0.3f), new(1f, 0.55f, 0.7f), new(0.7f, 0.55f, 1f) };
        for (int i = 0; i < 260; i++)
        {
            var position = new Vector2(rng.RandfRange(-half.X, half.X), rng.RandfRange(-half.Y, half.Y));
            if (IsOnRoad(position, margin: 10))
                continue;
            Color color = petals[rng.RandiRange(0, petals.Length - 1)];
            for (int p = 0; p < 4; p++)
                DrawCircle(position + Vector2.FromAngle(p * Mathf.Pi / 2) * 2.5f, 2f, color);
            DrawCircle(position, 1.5f, new Color(0.95f, 0.7f, 0.2f));
        }
    }
}
