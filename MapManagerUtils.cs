using CounterStrikeSharp.API;

namespace OnlyRealMapManager;

public static class MapManagerUtils
{
    public static Dictionary<string, string> LoadMapCycle(string fileName, string prefix)
    {
        string path = Path.Combine(Server.GameDirectory, "csgo", fileName);
        Dictionary<string, string> maps = new();
        
        Console.WriteLine($"{prefix} Reading: {path}");
        if (!File.Exists(path))
        {
            Console.WriteLine($"{prefix} mapcycle.txt not found");
            return maps;
        }

        maps.Clear();

        foreach (string line in File.ReadAllLines(path))
        {
            string l = line.Trim();
            if (l.Length == 0 || l.StartsWith("//")) continue;
            
            string[] section = l.Split('=', 2, StringSplitOptions.TrimEntries);
            maps[section[0]] = section.Length > 1 ? section[1] : section[0];
        }
        Console.WriteLine($"{prefix} Loaded {maps.Count} maps");
        return maps;
    }
}