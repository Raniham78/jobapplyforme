using System.Diagnostics;
using System.Text.Json;
using JobPilot.Models;

namespace JobPilot.Services;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ProfileStore(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        ProfilePath = Path.Combine(dataDirectory, "profile.json");
        Directory.CreateDirectory(DataDirectory);
    }

    public string DataDirectory { get; }
    public string ProfilePath { get; }
    public string BrowserProfilePath => Path.Combine(DataDirectory, "EdgeProfile");
    public string DatabasePath => Path.Combine(DataDirectory, "jobpilot.db");

    public async Task<UserProfile> LoadOrCreateAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DataDirectory);

        if (!File.Exists(ProfilePath))
        {
            var initialProfile = new UserProfile();
            var json = JsonSerializer.Serialize(initialProfile, JsonOptions);
            await File.WriteAllTextAsync(ProfilePath, json, cancellationToken);
        }

        var content = await File.ReadAllTextAsync(ProfilePath, cancellationToken);
        return JsonSerializer.Deserialize<UserProfile>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException($"Could not read profile: {ProfilePath}");
    }

    public void OpenProfileInNotepad()
    {
        Directory.CreateDirectory(DataDirectory);
        if (!File.Exists(ProfilePath))
            File.WriteAllText(ProfilePath, JsonSerializer.Serialize(new UserProfile(), JsonOptions));

        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{ProfilePath}\"")
        {
            UseShellExecute = true
        });
    }
}
