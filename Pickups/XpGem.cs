using Godot;

// An experience gem dropped by enemies. Flies to the player when close, gives XP on contact.
public partial class XpGem : Area2D
{
    [Export] public float MagnetRadius = 90f;
    [Export] public float Speed = 300f;

    // Set by the Enemy that drops it.
    public int Value = 1;

    private Player _player;
    private bool _collected;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        BodyEntered += OnBodyEntered;
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
}
