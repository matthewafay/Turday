using System.Text.Json;
using System.Text.Json.Serialization;

namespace TurDay.Save;

public sealed class SaveData
{
    public int Version { get; set; } = 1;
    public int Coins { get; set; }
    public int BestStage { get; set; }
    public int HighScore { get; set; }
    public string CurrentCharacter { get; set; } = "classic";
    public List<string> Unlocked { get; set; } = new() { "classic" };
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SaveData))]
internal partial class SaveJsonContext : JsonSerializerContext { }

public static class SaveStore
{
    public static string Directory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TurDay");

    public static string FilePath => Path.Combine(Directory, "save.json");

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new SaveData();
            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize(json, SaveJsonContext.Default.SaveData);
            return data ?? new SaveData();
        }
        catch
        {
            return new SaveData();
        }
    }

    public static void Save(SaveData data)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var tmp = FilePath + ".tmp";
        var json = JsonSerializer.Serialize(data, SaveJsonContext.Default.SaveData);
        File.WriteAllText(tmp, json);
        if (File.Exists(FilePath)) File.Delete(FilePath);
        File.Move(tmp, FilePath);
    }
}
