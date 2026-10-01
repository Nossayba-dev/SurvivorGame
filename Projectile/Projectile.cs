using Godot;

// A bullet that flies in a straight line and damages the first enemy it touches.
public partial class Projectile : Area2D
{
    [Export] public float Speed = 400f;
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
