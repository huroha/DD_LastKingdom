using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveSystem 
{
    private const string SaveFileName = "gamesave.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static void Save(GameSaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }
    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        string json = File.ReadAllText(SavePath);
        return JsonConvert.DeserializeObject<GameSaveData>(json);
    }
}
