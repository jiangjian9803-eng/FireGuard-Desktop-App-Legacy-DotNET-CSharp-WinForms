using System.Text;
using FireGuard360.Legacy.WinForms.Models;

namespace FireGuard360.Legacy.WinForms.Services;

public static class ReportService
{
    public static void ExportText(WorkflowResult result, string path)
    {
        var report = new StringBuilder()
            .AppendLine("FIREGUARD DESKTOP APP — INCIDENT REPORT")
            .AppendLine($"Generated: {DateTime.Now:G}")
            .AppendLine($"Region: {result.Sensor.RegionId}")
            .AppendLine($"Coordinates: {result.Sensor.Location.Latitude}, {result.Sensor.Location.Longitude}")
            .AppendLine($"Conditions: {result.Sensor.Temperature} C | {result.Sensor.Humidity}% RH | {result.Sensor.WindSpeed} km/h wind")
            .AppendLine($"Fuel dryness: {result.Sensor.FuelDryness}% | Wind direction: {result.Sensor.WindDirection} degrees")
            .AppendLine($"Risk: {result.Risk.RiskScore:F1}% ({result.Risk.RiskLevel})")
            .AppendLine($"Risk factors: {result.Risk.Explanation}")
            .AppendLine($"Projected spread: {result.Spread.SpreadPath}")
            .AppendLine($"Impact: {result.Impact.ImpactedAreas}; {result.Impact.PopulationAtRisk:N0} people; {result.Impact.CriticalAssets}")
            .AppendLine($"Resources: {result.Response.RequiredCrews} crews | {result.Response.RequiredEngines} engines | {result.Response.RequiredAircraft} aircraft")
            .AppendLine($"Response: {result.Response.Recommendations}")
            .AppendLine()
            .AppendLine("EXECUTIVE SUMMARY")
            .AppendLine(result.Summary.Summary)
            .ToString();

        File.WriteAllText(path, report);
    }
}
