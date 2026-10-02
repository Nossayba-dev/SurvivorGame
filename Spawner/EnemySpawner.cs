using Godot;

// Creates enemies around the player at regular intervals.
// Every WaveDuration seconds the wave number goes up and enemies get stronger and more frequent.
public partial class EnemySpawner : Node
{
    [Signal] public delegate void EnemyKilledEventHandler(int scoreValue);
    [Signal] public delegate void WaveChangedEventHandler(int wave);

    [Export] public PackedScene EnemyScene;
    // Exported node references: assigned in Main.tscn by dragging nodes into the Inspector slots.
    [Export] public Node2D EnemyContainer;
    [Export] public World WorldNode;

    [Export] public float WaveDuration = 30f;
    // How far from the player enemies appear (just outside the visible area).
    [Export] public float SpawnDistance = 420f;

    public int Wave { get; private set; } = 1;

    private Timer _spawnTimer;
    private Node2D _player;
    private float _waveTimer;
    // The space a new enemy needs: used to check that a spawn point is not inside a house or tree.
    private readonly CircleShape2D _spawnCheckShape = new() { Radius = 20 };

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        _spawnTimer = GetNode<Timer>("SpawnTimer");
        _spawnTimer.WaitTime = GetSpawnInterval();
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        _spawnTimer.Start();
    }

    // _Process runs every rendered frame. Good for timers and game logic that doesn't move bodies.
    public override void _Process(double delta)
    {
        _waveTimer += (float)delta;
        if (_waveTimer < WaveDuration)
            return;

        _waveTimer -= WaveDuration;
        Wave++;
        _spawnTimer.WaitTime = GetSpawnInterval();
        EmitSignal(SignalName.WaveChanged, Wave);
    }

    // Wave 1: one spawn every 1.2 s, then 15% faster each wave, never faster than 0.2 s.
    private float GetSpawnInterval()
    {
        return Mathf.Max(1.2f * Mathf.Pow(0.85f, Wave - 1), 0.2f);
    }

    private void OnSpawnTimerTimeout()
    {
        if (!IsInstanceValid(_player))
            return;

        // Wave 1-2: 1 enemy per spawn, wave 3-5: 2, wave 6-8: 3...
        int count = 1 + Wave / 3;
        for (int i = 0; i < count; i++)
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        Vector2? position = FindSpawnPosition();
        if (position == null)
            return;

        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.GlobalPosition = position.Value;

        // Stats depend on the wave. Set BEFORE AddChild, because Enemy._Ready reads MaxHealth.
        enemy.MaxHealth = 1 + Wave;
        enemy.Speed = Mathf.Min(70 + Wave * 10, 190);
        enemy.ScoreValue = 10 * Wave;

        enemy.Died += OnEnemyDied;
        EnemyContainer.AddChild(enemy);
    }

    // A random free point on a circle around the player, inside the map.
    // Returns null (Vector2? is a "nullable" struct) if 10 tries all landed on an obstacle.
    private Vector2? FindSpawnPosition()
    {
        Vector2 halfMap = WorldNode.MapSize / 2 - new Vector2(80, 80);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            Vector2 position = _player.GlobalPosition + Vector2.FromAngle(angle) * SpawnDistance;
            position = position.Clamp(-halfMap, halfMap);

            if (IsFree(position))
                return position;
        }
        return null;
    }

    // Asks the physics engine: "would a circle of radius 20 here touch a wall, house or tree?"
    private bool IsFree(Vector2 position)
    {
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = _spawnCheckShape,
            Transform = new Transform2D(0, position),
            CollisionMask = 1,   // layer 1 = world (walls, houses, trees)
        };
        PhysicsDirectSpaceState2D space = GetViewport().World2D.DirectSpaceState;
        return space.IntersectShape(query, 1).Count == 0;
    }

    // Relays each enemy's Died signal as a single spawner-level signal.
    private void OnEnemyDied(int scoreValue)
    {
        EmitSignal(SignalName.EnemyKilled, scoreValue);
    }
}
