using Godot;

// A basic enemy that walks straight towards the player and can be damaged.
public partial class Enemy : CharacterBody2D
{
    [Export] public float Speed = 100f;
    [Export] public int MaxHealth = 3;

    // The node we are chasing. Found once in _Ready.
    private Node2D _target;
    private int _health;

    // Called once by Godot when the node enters the scene tree, after its children are ready.
    public override void _Ready()
    {
        _health = MaxHealth;

        // The Player adds itself to the "player" group in Player.tscn.
        _target = GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    public override void _PhysicsProcess(double delta)
    {
        // False if there is no player, or if it was deleted (e.g. after it dies).
        if (!IsInstanceValid(_target))
            return;

        Vector2 direction = GlobalPosition.DirectionTo(_target.GlobalPosition);
        Velocity = direction * Speed;
        MoveAndSlide();
    }

    public void TakeDamage(int amount)
    {
        _health -= amount;

        // Flash: make the enemy very bright, then fade back to normal in 0.1 s.
        Modulate = new Color(3, 3, 3);
        CreateTween().TweenProperty(this, "modulate", Colors.White, 0.1);

        if (_health <= 0)
            QueueFree();
    }
}
