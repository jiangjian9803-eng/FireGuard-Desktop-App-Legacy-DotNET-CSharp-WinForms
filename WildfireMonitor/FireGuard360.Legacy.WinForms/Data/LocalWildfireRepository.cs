using System.Text.Json;
using FireGuard360.Legacy.WinForms.Models;

namespace FireGuard360.Legacy.WinForms.Data;

public sealed class LocalWildfireRepository
{
    private readonly string _dataFile;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LocalWildfireRepository(string? dataFile = null)
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FireGuard360Legacy");
        Directory.CreateDirectory(dataDirectory);
        _dataFile = dataFile ?? Path.Combine(dataDirectory, "wildfire-records.json");
    }

    public IReadOnlyList<WorkflowResult> Load()
    {
        if (!File.Exists(_dataFile))
            return Array.Empty<WorkflowResult>();

        var json = File.ReadAllText(_dataFile);
        return JsonSerializer.Deserialize<List<WorkflowResult>>(json, _jsonOptions)
               ?? new List<WorkflowResult>();
    }

    public void Save(WorkflowResult result)
    {
        var records = Load().ToList();
        records.Add(result);
        File.WriteAllText(_dataFile, JsonSerializer.Serialize(records, _jsonOptions));
    }

    public string DataFile => _dataFile;
}
