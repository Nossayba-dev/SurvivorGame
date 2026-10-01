using Godot;

// A basic enemy that walks straight towards the player.
public partial class Enemy : CharacterBody2D
{
    [Export] public float Speed = 100f;

    // The node we are chasing. Found once in _Ready.
    private Node2D _target;

    // Called once by Godot when the node enters the scene tree, after its children are ready.
    public override void _Ready()
    {
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
}
