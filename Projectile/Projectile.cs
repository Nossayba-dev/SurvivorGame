using Godot;

// A bullet that flies in a straight line and damages the first enemy it touches.
public partial class Projectile : Area2D
{
    [Export] public float Speed = 600f;
    [Export] public int Damage = 1;
    [Export] public float MaxDistance = 500f;

    // Set by the Weapon before the projectile is added to the tree.
    public Vector2 Direction = Vector2.Right;

    private float _traveled;
    private bool _hasHit;

    public override void _Ready()
    {
        // Subscribe to the Area2D "body_entered" signal, like a C# event.
        BodyEntered += OnBodyEntered;

        // Point the visual in the flight direction.
        Rotation = Direction.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Area2D has no MoveAndSlide, so we move it ourselves and must multiply by delta.
        float step = Speed * (float)delta;
        Position += Direction * step;

        _traveled += step;
        if (_traveled >= MaxDistance)
            QueueFree();
    }

    // Drawn once (nothing changes): a light trail behind a small brass bullet, pointing along +X.
    // The node's Rotation turns the drawing in the flight direction.
    public override void _Draw()
    {
        DrawLine(new Vector2(-18, 0), new Vector2(-3, 0), new Color(1f, 0.9f, 0.55f, 0.25f), 3f);
        DrawLine(new Vector2(-9, 0), new Vector2(-3, 0), new Color(1f, 0.95f, 0.7f, 0.55f), 2.5f);
        this.DrawLimb(new Vector2(-2.5f, 0), new Vector2(2.5f, 0), 3.5f, new Color(0.95f, 0.75f, 0.3f), 1f);
    }

    private void OnBodyEntered(Node2D body)
    {
        // Several bodies can enter during the same physics step; only hit the first one.
        if (_hasHit)
            return;
        _hasHit = true;

        if (body is Enemy enemy)
            enemy.TakeDamage(Damage);

        // Walls just destroy the projectile.
        QueueFree();
    }
}
