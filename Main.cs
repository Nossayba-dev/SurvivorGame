using Godot;

// The game manager. Attached to the root of Main.tscn.
// It connects the other nodes together: they don't know about each other, Main does.
public partial class Main : Node2D
{
    private Player _player;
    private EnemySpawner _spawner;
    private Hud _hud;

    private float _elapsed;
    private int _score;
    private int _highScore;

    public override void _Ready()
    {
        // Children are ready before their parent, so these nodes are fully set up already.
        _player = GetNode<Player>("Player");
        _spawner = GetNode<EnemySpawner>("EnemySpawner");
        _hud = GetNode<Hud>("Hud");

        // Wire signals to HUD methods. The method signatures match the signal parameters.
        _player.HealthChanged += _hud.SetHealth;
        _player.XpChanged += _hud.SetXp;
        _spawner.WaveChanged += _hud.SetWave;
        _spawner.EnemyKilled += OnEnemyKilled;
        _player.Died += OnPlayerDied;

        _highScore = HighScore.Load();

        // Signals only report changes, so show the starting values once.
        _hud.SetHealth(_player.Health, _player.MaxHealth);
        _hud.SetXp(_player.Xp, _player.XpToNextLevel, _player.Level);
        _hud.SetWave(_spawner.Wave);
        _hud.SetScore(_score, _highScore);
    }

    public override void _Process(double delta)
    {
        if (_player.IsDead)
            return;

        _elapsed += (float)delta;
        _hud.SetTime(_elapsed);
    }

    private void OnEnemyKilled(int scoreValue)
    {
        _score += scoreValue;
        // While playing, "Best" shows the new record live as soon as it is beaten.
        _hud.SetScore(_score, Mathf.Max(_score, _highScore));
    }

    private void OnPlayerDied()
    {
        if (_score > _highScore)
        {
            _highScore = _score;
            HighScore.Save(_highScore);
        }
    }
}
