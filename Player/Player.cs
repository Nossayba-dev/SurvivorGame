using System;
using Godot;

// The player character. Reads keyboard input, moves the body, and owns the player's health.
public partial class Player : CharacterBody2D
{
    // Custom signals. Godot generates a C# event from each delegate: HealthChanged, Died.
    // The name must end with "EventHandler".
    [Signal] public delegate void HealthChangedEventHandler(int health, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    // Movement speed in pixels per second. [Export] makes it editable in the Inspector.
    [Export] public float Speed = 200f;
    [Export] public int MaxHealth = 10;
    // After taking a hit, the player can't be hit again for this many seconds.
    [Export] public float InvulnerabilityTime = 0.5f;

    // Public to read, private to change: only the Player decides its own health.
    public int Health { get; private set; }
    public bool IsDead { get; private set; }

    private Area2D _hurtbox;
    private Weapon _weapon;
    private float _invulnerableTimer;

    public override void _Ready()
    {
        Health = MaxHealth;
        _hurtbox = GetNode<Area2D>("Hurtbox");
        _weapon = GetNode<Weapon>("Weapon");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
            return;

        // Returns a direction vector from the 4 input actions, already normalized
        // (moving diagonally is not faster than moving straight).
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        Velocity = direction * Speed;

        // Moves the body using Velocity and handles collisions. Applies delta internally.
        MoveAndSlide();

        _invulnerableTimer -= (float)delta;
        if (_invulnerableTimer <= 0)
            CheckEnemyContact();
    }

    // Takes damage from the first enemy touching the hurtbox, if any.
    private void CheckEnemyContact()
    {
        foreach (Node2D body in _hurtbox.GetOverlappingBodies())
        {
            if (body is Enemy enemy)
            {
                TakeDamage(enemy.Damage);
                return;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;

        Health = Math.Max(Health - amount, 0);
        _invulnerableTimer = InvulnerabilityTime;

        // Flash red, then fade back to normal.
        Modulate = new Color(1, 0.3f, 0.3f);
        CreateTween().TweenProperty(this, "modulate", Colors.White, 0.3);

        EmitSignal(SignalName.HealthChanged, Health, MaxHealth);

        if (Health == 0)
            Die();
    }

    private void Die()
    {
        IsDead = true;
        _weapon.Stop();
        Hide();
        EmitSignal(SignalName.Died);
    }
}
