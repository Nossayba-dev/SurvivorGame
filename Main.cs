using Godot;

// The game manager. Attached to the root of Main.tscn.
// It connects the other nodes together: they don't know about each other, Main does.
public partial class Main : Node2D
{
    private Player _player;
    private EnemySpawner _spawner;
    private Hud _hud;
    private GameOverScreen _gameOverScreen;

    private float _elapsed;
    private int _score;
    private int _highScore;

    public override void _Ready()
    {
        // Children are ready before their parent, so these nodes are fully set up already.
        _player = GetNode<Player>("Entities/Player");
        _spawner = GetNode<EnemySpawner>("EnemySpawner");
        _hud = GetNode<Hud>("Hud");
        _gameOverScreen = GetNode<GameOverScreen>("GameOverScreen");

        // Wire signals to HUD methods. The method signatures match the signal parameters.
        _player.HealthChanged += _hud.SetHealth;
        _player.XpChanged += _hud.SetXp;
        _spawner.WaveChanged += _hud.SetWave;
        _spawner.EnemyKilled += OnEnemyKilled;
        _player.Died += OnPlayerDied;
        _gameOverScreen.RestartRequested += OnRestartRequested;

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
        bool isNewHighScore = _score > _highScore;
        if (isNewHighScore)
        {
            _highScore = _score;
            HighScore.Save(_highScore);
        }

        // Freeze every node whose process_mode is "Inherit" or "Pausable".
        // GameOverScreen uses "Always", so its button still works.
        GetTree().Paused = true;
        _gameOverScreen.ShowResults(_score, _highScore, isNewHighScore, _elapsed, _player.Level);
    }

    private void OnRestartRequested()
    {
        GetTree().Paused = false;
        // Destroys the whole Main scene and loads it again from Main.tscn: a fresh game.
        GetTree().ReloadCurrentScene();
    }
}
