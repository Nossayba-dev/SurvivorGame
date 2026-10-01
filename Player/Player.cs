using Godot;

// The player character. Reads keyboard input and moves the body.
public partial class Player : CharacterBody2D
{
    // Movement speed in pixels per second. [Export] makes it editable in the Inspector.
    [Export] public float Speed = 200f;

    public override void _PhysicsProcess(double delta)
    {
        // Returns a direction vector from the 4 input actions, already normalized
        // (moving diagonally is not faster than moving straight).
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        Velocity = direction * Speed;

        // Moves the body using Velocity and handles collisions. Applies delta internally.
        MoveAndSlide();
    }
}
