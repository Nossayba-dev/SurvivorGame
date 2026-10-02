using Godot;

// A basic enemy that walks straight towards the player and can be damaged.
public partial class Enemy : CharacterBody2D
{
    // Emitted once when the enemy dies. The spawner listens to it to count the score.
    [Signal] public delegate void DiedEventHandler(int scoreValue);

    [Export] public float Speed = 100f;
    [Export] public int MaxHealth = 3;
    // Damage dealt to the player on contact.
    [Export] public int Damage = 1;
    [Export] public int XpValue = 1;
    [Export] public int ScoreValue = 10;
    // The gem scene to drop on death. Assigned in Enemy.tscn.
    [Export] public PackedScene XpGemScene;

    // Torn shirt colors picked at random so the zombies don't all look the same.
    // static readonly: one shared array for all enemies, created once.
    private static readonly Color[] ShirtColors =
    {
        new(0.45f, 0.36f, 0.52f), new(0.55f, 0.3f, 0.28f), new(0.3f, 0.42f, 0.5f),
        new(0.5f, 0.48f, 0.3f), new(0.35f, 0.35f, 0.38f),
    };

    // The node we are chasing. Found once in _Ready.
    private Node2D _target;
    private int _health;
    private bool _isDead;

    // Called once by Godot when the node enters the scene tree, after its children are ready.
    public override void _Ready()
    {
        _health = MaxHealth;

        var visual = GetNode<CharacterVisual>("Visual");
        visual.ShirtColor = ShirtColors[GD.RandRange(0, ShirtColors.Length - 1)];

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
        // Two projectiles can hit in the same frame: don't die twice.
        if (_isDead)
            return;

        _health -= amount;

        // Flash: make the enemy very bright, then fade back to normal in 0.1 s.
        Modulate = new Color(3, 3, 3);
        CreateTween().TweenProperty(this, "modulate", Colors.White, 0.1);

        if (_health <= 0)
            Die();
    }

    private void Die()
    {
        _isDead = true;
        EmitSignal(SignalName.Died, ScoreValue);
        DropXpGem();
        QueueFree();
    }

    private void DropXpGem()
    {
        var gem = XpGemScene.Instantiate<XpGem>();
        gem.Value = XpValue;
        gem.GlobalPosition = GlobalPosition;

        // We are inside a physics callback (a projectile's BodyEntered).
        // Godot forbids adding physics objects at that moment, so we add it at the end of the frame.
        GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, gem);
    }
}
