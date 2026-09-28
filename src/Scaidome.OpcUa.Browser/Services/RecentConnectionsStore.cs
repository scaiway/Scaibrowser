using System.IO;
using System.Text.Json;

namespace Scaidome.OpcUa.Browser.Services;

/// <summary>A previously used connection. Passwords are intentionally not persisted.</summary>
public sealed record RecentConnection(string ServerUrl, bool UseSecurity, string? UserName, DateTime LastUsed)
{
    public string DisplayText => ServerUrl
        + (UseSecurity ? "  •  secure" : "")
        + (string.IsNullOrEmpty(UserName) ? "" : $"  •  {UserName}");
}

public sealed class RecentConnectionsStore
{
    private const int MaxEntries = 10;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public RecentConnectionsStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scaidome.OpcUa.Browser", "recent-connections.json"))
    {
    }

    public RecentConnectionsStore(string filePath)
    {
        _filePath = filePath;
    }

    public List<RecentConnection> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];

            return JsonSerializer.Deserialize<List<RecentConnection>>(File.ReadAllText(_filePath)) ?? [];
        }
        catch
        {
            // A corrupt or unreadable file shouldn't stop the app from starting.
            return [];
        }
    }

    /// <summary>Puts the connection first (replacing an identical older entry) and returns the updated list.</summary>
    public List<RecentConnection> Add(RecentConnection connection)
    {
        var list = Load();

        list.RemoveAll(c => string.Equals(c.ServerUrl, connection.ServerUrl, StringComparison.OrdinalIgnoreCase)
            && c.UseSecurity == connection.UseSecurity
            && c.UserName == connection.UserName);
        list.Insert(0, connection);

        if (list.Count > MaxEntries)
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(list, JsonOptions));
        }
        catch
        {
            // Best effort; failing to save history shouldn't fail the connect.
        }

        return list;
    }
}
