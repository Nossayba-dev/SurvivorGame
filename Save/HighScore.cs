using Godot;

// Loads and saves the best score on disk.
// A plain static C# class, not a Node: it has no position, no scene, and needs no lifecycle.
public static class HighScore
{
    // "user://" is a per-user folder Godot manages for save data.
    // On Windows: %APPDATA%\Godot\app_userdata\SurvivorGame\
    private const string SavePath = "user://save.cfg";
    private const string Section = "scores";
    private const string Key = "high_score";

    public static int Load()
    {
        var config = new ConfigFile();

        // No file yet (first time playing): the high score is 0.
        if (config.Load(SavePath) != Error.Ok)
            return 0;

        return (int)config.GetValue(Section, Key, 0);
    }

    public static void Save(int score)
    {
        var config = new ConfigFile();
        config.SetValue(Section, Key, score);
        config.Save(SavePath);
    }
}
