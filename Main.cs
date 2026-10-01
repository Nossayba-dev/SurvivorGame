using Godot;

// The game manager. Attached to the root of Main.tscn.
// It connects the other nodes together: they don't know about each other, Main does.
public partial class Main : Node2D
{
    private Player _player;
    private EnemySpawner _spawner;
    private Hud _hud;

    private float _elapsed;

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

        // Signals only report changes, so show the starting values once.
        _hud.SetHealth(_player.Health, _player.MaxHealth);
        _hud.SetXp(_player.Xp, _player.XpToNextLevel, _player.Level);
        _hud.SetWave(_spawner.Wave);
    }

    public override void _Process(double delta)
    {
        if (_player.IsDead)
            return;

        _elapsed += (float)delta;
        _hud.SetTime(_elapsed);
    }
}
