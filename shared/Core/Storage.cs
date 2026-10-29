using System.Text.Json;

namespace Zen.Core;

public static class Storage
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = false, PropertyNameCaseInsensitive = true };
    public static string DataPath(string name)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZenOop");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, name + ".json");
    }
    public static void Save<T>(string path, T value)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temporary = full + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, Options));
        File.Move(temporary, full, true);
    }
    public static T Load<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException("Файл не содержит данных.");
}
