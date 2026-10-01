using System;
using Godot;

// The on-screen display. Only shows values: it never changes the game itself.
// Main calls these Set... methods whenever something changes.
public partial class Hud : CanvasLayer
{
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private ProgressBar _xpBar;
    private Label _levelLabel;
    private Label _waveLabel;
    private Label _timeLabel;
    private Label _scoreLabel;
    private Label _highScoreLabel;

    public override void _Ready()
    {
        // "%Name" finds a node marked "Access as Unique Name" anywhere in this scene,
        // so moving it inside the layout does not break the code.
        _healthBar = GetNode<ProgressBar>("%HealthBar");
        _healthLabel = GetNode<Label>("%HealthLabel");
        _xpBar = GetNode<ProgressBar>("%XpBar");
        _levelLabel = GetNode<Label>("%LevelLabel");
        _waveLabel = GetNode<Label>("%WaveLabel");
        _timeLabel = GetNode<Label>("%TimeLabel");
        _scoreLabel = GetNode<Label>("%ScoreLabel");
        _highScoreLabel = GetNode<Label>("%HighScoreLabel");
    }

    public void SetHealth(int health, int maxHealth)
    {
        _healthBar.MaxValue = maxHealth;
        _healthBar.Value = health;
        _healthLabel.Text = $"{health} / {maxHealth}";
    }

    public void SetXp(int xp, int xpToNextLevel, int level)
    {
        _xpBar.MaxValue = xpToNextLevel;
        _xpBar.Value = xp;
        _levelLabel.Text = $"Level {level}";
    }

    public void SetWave(int wave)
    {
        _waveLabel.Text = $"Wave {wave}";
    }

    public void SetScore(int score, int highScore)
    {
        _scoreLabel.Text = $"Score {score}";
        _highScoreLabel.Text = $"Best {highScore}";
    }

    public void SetTime(float seconds)
    {
        // TimeSpan is plain .NET: formats 83.4 seconds as "1:23".
        _timeLabel.Text = TimeSpan.FromSeconds(seconds).ToString(@"m\:ss");
    }
}
