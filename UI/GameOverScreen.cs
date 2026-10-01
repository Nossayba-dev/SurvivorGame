using System;
using Godot;

// The screen shown when the player dies. Displays the results and offers a restart.
// It does not restart the game itself: it emits a signal and lets Main decide.
public partial class GameOverScreen : CanvasLayer
{
    [Signal] public delegate void RestartRequestedEventHandler();

    private Label _scoreLabel;
    private Label _highScoreLabel;
    private Label _newRecordLabel;
    private Label _statsLabel;
    private Button _restartButton;

    public override void _Ready()
    {
        _scoreLabel = GetNode<Label>("%ScoreLabel");
        _highScoreLabel = GetNode<Label>("%HighScoreLabel");
        _newRecordLabel = GetNode<Label>("%NewRecordLabel");
        _statsLabel = GetNode<Label>("%StatsLabel");
        _restartButton = GetNode<Button>("%RestartButton");

        // A lambda: a small anonymous method, written inline.
        _restartButton.Pressed += () => EmitSignal(SignalName.RestartRequested);

        Hide();
    }

    public void ShowResults(int score, int highScore, bool isNewHighScore, float survivedSeconds, int level)
    {
        _scoreLabel.Text = $"Score: {score}";
        _highScoreLabel.Text = $"Best: {highScore}";
        _newRecordLabel.Visible = isNewHighScore;
        _statsLabel.Text = $"Survived {TimeSpan.FromSeconds(survivedSeconds):m\\:ss}  ·  Level {level}";

        Show();

        // Keyboard focus on the button, so Enter / Space restarts without the mouse.
        _restartButton.GrabFocus();
    }
}
