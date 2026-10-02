using Godot;

// The player's pistol. Every frame it turns towards the nearest enemy; on each Timer tick it fires.
// It draws itself (gun, hand, sleeve, muzzle flash) with code.
// Attach as a child of the Player, at chest height. The gun points along its local +X axis.
public partial class Weapon : Node2D
{
    // The scene to create for each shot. Assigned in the Inspector (Player.tscn).
    [Export] public PackedScene ProjectileScene;
    [Export] public float FireInterval = 0.6f;
    [Export] public float Range = 400f;
    [Export] public int Damage = 1;
    // The fastest the weapon can ever fire, no matter the level.
    [Export] public float MinFireInterval = 0.15f;
    // How fast the gun turns towards its target, in radians per second.
    [Export] public float TurnSpeed = 14f;
    // Colors of the arm holding the gun: should match the player's skin and shirt.
    [Export] public Color HandColor = new(1f, 0.82f, 0.66f);
    [Export] public Color SleeveColor = new(0.23f, 0.5f, 0.9f);

    // How far in front of the chest the gun is held.
    private const float HoldDistance = 7f;
    // We aim at the enemy's body, a bit above its feet (its origin).
    private static readonly Vector2 TargetOffset = new(0, -8);

    // Normalized direction the gun points at. Read by the Player to turn the character.
    public Vector2 AimDirection { get; private set; } = Vector2.Right;
    public bool HasTarget => IsInstanceValid(_target);

    private Timer _timer;
    private Node2D _target;
    private Node2D _characterVisual;
    // Pixels the gun is pushed back after a shot; goes back to 0 quickly.
    private float _recoil;
    // The muzzle flash is visible while this is above 0.
    private float _flashTimer;

    // Where bullets come out, in this node's coordinates. Moves with the recoil.
    private Vector2 MuzzlePosition => new(HoldDistance - _recoil + 26, -2);

    public override void _Ready()
    {
        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = FireInterval;
        _timer.Timeout += Fire;
        _timer.Start();

        _characterVisual = GetParent().GetNodeOrNull<Node2D>("Visual");
    }

    public void Stop()
    {
        _timer.Stop();
    }

    // Called by the Player on each level up.
    public void Upgrade(int newLevel)
    {
        // 10% faster each level.
        FireInterval = Mathf.Max(FireInterval * 0.9f, MinFireInterval);
        _timer.WaitTime = FireInterval;

        // +1 damage every 2 levels.
        if (newLevel % 2 == 0)
            Damage++;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _target = FindNearestEnemy();
        if (_target != null)
        {
            Vector2 wanted = GlobalPosition.DirectionTo(_target.GlobalPosition + TargetOffset);
            // Turn smoothly instead of snapping: move the angle towards the wanted angle,
            // by at most TurnSpeed * dt (RotateToward picks the shortest way around the circle).
            float angle = Mathf.RotateToward(AimDirection.Angle(), wanted.Angle(), TurnSpeed * dt);
            AimDirection = Vector2.FromAngle(angle);
        }

        // Rotating the node rotates everything it draws.
        Rotation = AimDirection.Angle();
        // Aiming left would show the gun upside down: mirror it vertically.
        Scale = new Vector2(1, AimDirection.X < 0 ? -1 : 1);
        UpdateDrawOrder();

        _recoil = Mathf.MoveToward(_recoil, 0, 40 * dt);
        _flashTimer -= dt;
        QueueRedraw();
    }

    // Siblings are drawn in tree order. Aiming up, the gun should be behind the body:
    // we move this node just before the character visual; otherwise just after it.
    private void UpdateDrawOrder()
    {
        if (_characterVisual == null)
            return;

        bool shouldBeBehind = AimDirection.Y < -0.5f;
        bool isBehind = GetIndex() < _characterVisual.GetIndex();
        if (shouldBeBehind != isBehind)
            GetParent().MoveChild(this, _characterVisual.GetIndex());
    }

    private void Fire()
    {
        if (!HasTarget)
            return;

        // ToGlobal converts a point from this node's coordinates (rotated, mirrored) to world coordinates.
        Vector2 muzzle = ToGlobal(MuzzlePosition);

        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = muzzle;
        projectile.Direction = muzzle.DirectionTo(_target.GlobalPosition + TargetOffset);
        projectile.Damage = Damage;

        // Add to the main scene, not to the Player:
        // as a child of the Player it would move along with the Player.
        GetTree().CurrentScene.AddChild(projectile);

        _recoil = 4f;
        _flashTimer = 0.06f;
    }

    // Returns the closest enemy within Range, or null if there is none.
    private Node2D FindNearestEnemy()
    {
        Node2D nearest = null;
        float nearestDistance = Range;

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Node2D enemy)
                continue;

            float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    public override void _Draw()
    {
        float x = HoldDistance - _recoil;
        Color metal = new(0.27f, 0.29f, 0.33f);

        // Sleeve from the chest to the hand: the player's arm.
        this.DrawLimb(new Vector2(-2, 2), new Vector2(x + 4, 3), 5, SleeveColor);

        // Grip (handle).
        DrawColoredPolygon(new[]
        {
            new Vector2(x + 2, 0), new Vector2(x + 8, 0), new Vector2(x + 6, 8), new Vector2(x, 8),
        }, new Color(0.32f, 0.2f, 0.14f));

        // Slide (body) and barrel, with a light reflection on top.
        this.DrawOutlinedRect(new Rect2(x, -5, 20, 6), metal, 1.5f);
        this.DrawOutlinedRect(new Rect2(x + 19, -4, 8, 4), metal.Darkened(0.3f), 1.5f);
        DrawLine(new Vector2(x + 2, -3.5f), new Vector2(x + 17, -3.5f), metal.Lightened(0.35f), 1f);

        // Trigger guard.
        DrawArc(new Vector2(x + 10, 1.5f), 3f, 0, Mathf.Pi, 6, DrawUtils.Outline, 1.2f);

        // The hand holding the grip.
        this.DrawOutlinedCircle(new Vector2(x + 4, 3), 3.3f, HandColor, 1.2f);

        if (_flashTimer > 0)
            DrawMuzzleFlash(MuzzlePosition + new Vector2(2, 0));
    }

    // A star of 8 points (alternating long and short) with a bright center.
    private void DrawMuzzleFlash(Vector2 center)
    {
        var points = new Vector2[8];
        for (int i = 0; i < points.Length; i++)
        {
            float radius = i % 2 == 0 ? 8f : 3.5f;
            points[i] = center + Vector2.FromAngle(Mathf.Tau * i / points.Length) * radius;
        }
        DrawColoredPolygon(points, new Color(1f, 0.6f, 0.15f));
        DrawCircle(center, 3f, new Color(1f, 0.95f, 0.6f));
    }
}
