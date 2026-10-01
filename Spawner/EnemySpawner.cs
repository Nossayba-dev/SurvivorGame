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
        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.GlobalPosition = GetSpawnPosition();

        // Stats depend on the wave. Set BEFORE AddChild, because Enemy._Ready reads MaxHealth.
        enemy.MaxHealth = 2 + Wave;
        enemy.Speed = Mathf.Min(70 + Wave * 10, 190);
        enemy.ScoreValue = 10 * Wave;

        enemy.Died += OnEnemyDied;
        EnemyContainer.AddChild(enemy);
    }

    // A random point on a circle around the player, kept inside the map walls.
    private Vector2 GetSpawnPosition()
    {
        float angle = GD.Randf() * Mathf.Tau;
        Vector2 position = _player.GlobalPosition + Vector2.FromAngle(angle) * SpawnDistance;

        Vector2 halfMap = WorldNode.MapSize / 2 - new Vector2(40, 40);
        return position.Clamp(-halfMap, halfMap);
    }

    // Relays each enemy's Died signal as a single spawner-level signal.
    private void OnEnemyDied(int scoreValue)
    {
        EmitSignal(SignalName.EnemyKilled, scoreValue);
    }
}
