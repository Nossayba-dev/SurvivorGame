using System;
using Godot;

// The player character. Reads keyboard input, moves the body, and owns the player's health.
public partial class Player : CharacterBody2D
{
    // Custom signals. Godot generates a C# event from each delegate (HealthChanged, XpChanged...).
    // The name must end with "EventHandler".
    [Signal] public delegate void HealthChangedEventHandler(int health, int maxHealth);
    [Signal] public delegate void XpChangedEventHandler(int xp, int xpToNextLevel, int level);
    [Signal] public delegate void LeveledUpEventHandler(int level);
    [Signal] public delegate void DiedEventHandler();

    // Movement speed in pixels per second. [Export] makes it editable in the Inspector.
    [Export] public float Speed = 200f;
    [Export] public int MaxHealth = 10;
    // After taking a hit, the player can't be hit again for this many seconds.
    [Export] public float InvulnerabilityTime = 0.5f;

    // Public to read, private to change: only the Player decides its own health.
    public int Health { get; private set; }
    public bool IsDead { get; private set; }
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }

    // Computed property: recalculated every time it is read. Level 1 needs 3 XP, then 5, 7, 9...
    public int XpToNextLevel => 3 + (Level - 1) * 2;

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

    public void AddXp(int amount)
    {
        if (IsDead)
            return;

        Xp += amount;

        // "while" and not "if": a big XP gain could give several levels at once.
        while (Xp >= XpToNextLevel)
        {
            Xp -= XpToNextLevel;
            LevelUp();
        }

        EmitSignal(SignalName.XpChanged, Xp, XpToNextLevel, Level);
    }

    private void LevelUp()
    {
        Level++;
        _weapon.Upgrade(Level);

        // Each level: +1 max health and heal 3.
        MaxHealth++;
        Health = Math.Min(Health + 3, MaxHealth);
        EmitSignal(SignalName.HealthChanged, Health, MaxHealth);

        EmitSignal(SignalName.LeveledUp, Level);
    }

    private void Die()
    {
        IsDead = true;
        _weapon.Stop();
        Hide();
        EmitSignal(SignalName.Died);
    }
}
