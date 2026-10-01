using Godot;

// Automatically fires projectiles at the nearest enemy. Attach as a child of the Player.
public partial class Weapon : Node2D
{
    // The scene to create for each shot. Assigned in the Inspector (Player.tscn).
    [Export] public PackedScene ProjectileScene;
    [Export] public float FireInterval = 0.8f;
    [Export] public float Range = 400f;

    private Timer _timer;

    public override void _Ready()
    {
        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = FireInterval;
        _timer.Timeout += Fire;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void Fire()
    {
        Node2D target = FindNearestEnemy();
        if (target == null)
            return;

        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Direction = GlobalPosition.DirectionTo(target.GlobalPosition);

        // Add to the main scene, not to the Player:
        // as a child of the Player it would move along with the Player.
        GetTree().CurrentScene.AddChild(projectile);
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
}
