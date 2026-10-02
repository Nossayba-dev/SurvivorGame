using Godot;

// A piece of food dropped by enemies. Flies to the player when close, gives XP on contact.
// It draws itself with code: a random food (apple, drumstick, cheese or carrot) bobbing over a shadow.
public partial class XpGem : Area2D
{
    // An enum: a named set of choices. Easier to read than "food 0, 1, 2, 3".
    private enum FoodType { Apple, Drumstick, Cheese, Carrot }

    [Export] public float MagnetRadius = 90f;
    [Export] public float Speed = 300f;

    // Set by the Enemy that drops it.
    public int Value = 1;

    private Player _player;
    private bool _collected;
    private FoodType _food;
    // Time since the food appeared, used for the bobbing animation.
    private float _time;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        BodyEntered += OnBodyEntered;

        // Enum values are numbers underneath: pick 0..3 at random and cast it to FoodType.
        _food = (FoodType)GD.RandRange(0, 3);
        // Start each food at a different point of the bobbing, so they don't all move together.
        _time = GD.Randf() * 10f;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(_player))
            return;

        if (GlobalPosition.DistanceTo(_player.GlobalPosition) <= MagnetRadius)
            GlobalPosition = GlobalPosition.MoveToward(_player.GlobalPosition, Speed * (float)delta);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected || body is not Player player)
            return;

        _collected = true;
        player.AddXp(Value);
        QueueFree();
    }

    public override void _Draw()
    {
        // The shadow stays on the ground; the food floats above it, going up and down.
        float bob = Mathf.Sin(_time * 4f) * 2f;
        this.DrawEllipse(new Vector2(0, 6), new Vector2(7 - bob * 0.5f, 2.5f), new Color(0, 0, 0, 0.25f));

        Vector2 center = new(0, -4 + bob);

        // A switch statement: run the drawing code that matches the food type.
        switch (_food)
        {
            case FoodType.Apple:
                DrawApple(center);
                break;
            case FoodType.Drumstick:
                DrawDrumstick(center);
                break;
            case FoodType.Cheese:
                DrawCheese(center);
                break;
            case FoodType.Carrot:
                DrawCarrot(center);
                break;
        }
    }

    private void DrawApple(Vector2 c)
    {
        this.DrawOutlinedCircle(c, 7f, new Color(0.88f, 0.18f, 0.2f));
        // Stem, leaf and a white shine.
        DrawLine(c + new Vector2(0, -6), c + new Vector2(1, -10), new Color(0.4f, 0.25f, 0.12f), 2f);
        this.DrawEllipse(c + new Vector2(4, -9), new Vector2(3.5f, 1.8f), new Color(0.3f, 0.7f, 0.25f));
        DrawCircle(c + new Vector2(-3, -3), 1.8f, new Color(1, 1, 1, 0.7f));
    }

    private void DrawDrumstick(Vector2 c)
    {
        // The bone sticks out to the bottom-left, the meat is a big ellipse.
        this.DrawLimb(c + new Vector2(-2, 2), c + new Vector2(-9, 7), 3f, new Color(0.96f, 0.93f, 0.85f), 1.2f);
        this.DrawOutlinedCircle(c + new Vector2(-10, 6), 2f, new Color(0.96f, 0.93f, 0.85f), 1.2f);
        this.DrawOutlinedCircle(c + new Vector2(-8, 9), 2f, new Color(0.96f, 0.93f, 0.85f), 1.2f);
        this.DrawEllipse(c + new Vector2(2, -2), new Vector2(8.5f, 6.5f), DrawUtils.Outline);
        this.DrawEllipse(c + new Vector2(2, -2), new Vector2(7, 5), new Color(0.78f, 0.45f, 0.2f));
        DrawCircle(c + new Vector2(0, -4), 1.8f, new Color(1f, 0.8f, 0.5f, 0.8f));
    }

    private void DrawCheese(Vector2 c)
    {
        Vector2[] wedge = { c + new Vector2(-8, 5), c + new Vector2(8, 5), c + new Vector2(6, -6) };
        DrawColoredPolygon(wedge, new Color(1f, 0.8f, 0.25f));
        DrawPolyline(new[] { wedge[0], wedge[1], wedge[2], wedge[0] }, DrawUtils.Outline, 1.5f);
        // Holes.
        Color hole = new(0.85f, 0.6f, 0.12f);
        DrawCircle(c + new Vector2(2, 1), 1.8f, hole);
        DrawCircle(c + new Vector2(5, -2), 1.2f, hole);
        DrawCircle(c + new Vector2(-2, 3), 1f, hole);
    }

    private void DrawCarrot(Vector2 c)
    {
        Vector2[] body = { c + new Vector2(-3, -5), c + new Vector2(4, -4), c + new Vector2(1, 9) };
        DrawColoredPolygon(body, new Color(1f, 0.55f, 0.12f));
        DrawPolyline(new[] { body[0], body[1], body[2], body[0] }, DrawUtils.Outline, 1.5f);
        // Little lines on the carrot and green leaves on top.
        DrawLine(c + new Vector2(-1, 0), c + new Vector2(1.5f, 0), new Color(0.8f, 0.4f, 0.1f), 1f);
        DrawLine(c + new Vector2(0, 4), c + new Vector2(1.8f, 4), new Color(0.8f, 0.4f, 0.1f), 1f);
        Color leaf = new(0.3f, 0.7f, 0.25f);
        DrawLine(c + new Vector2(0.5f, -5), c + new Vector2(-2, -10), leaf, 2f);
        DrawLine(c + new Vector2(0.5f, -5), c + new Vector2(1, -11), leaf, 2f);
        DrawLine(c + new Vector2(0.5f, -5), c + new Vector2(4, -9.5f), leaf, 2f);
    }
}
