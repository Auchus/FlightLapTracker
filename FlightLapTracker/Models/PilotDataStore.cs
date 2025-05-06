using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class PilotDataStore
{
    private const string FilePath = "Results/pilots.json";

    public static void Save(IEnumerable<Pilot> pilots)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(pilots, options);
        File.WriteAllText(FilePath, json);
    }

    public static List<Pilot> Load()
    {
        if (!File.Exists(FilePath)) return new List<Pilot>();
        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<List<Pilot>>(json) ?? new List<Pilot>();
    }
}