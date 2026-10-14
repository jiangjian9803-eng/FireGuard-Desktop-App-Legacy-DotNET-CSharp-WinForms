using FireGuard360.Legacy.WinForms.Data;
using FireGuard360.Legacy.WinForms.Models;

namespace FireGuard360.Legacy.WinForms.Services;

public sealed class WildfireWorkflowService
{
    private static readonly IReadOnlyDictionary<string, string> Infrastructure =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["region-001"] = "Hospital, Power Plant",
            ["region-002"] = "School, Highway",
            ["region-003"] = "Farm, Water Reservoir"
        };

    private readonly LocalWildfireRepository _repository;

    public WildfireWorkflowService(LocalWildfireRepository repository) => _repository = repository;

    public WorkflowResult Execute(SensorData sensor, Action<string>? log = null)
    {
        ValidateSensorData(sensor);
        log?.Invoke("1/7 DataCollector: sensor reading validated.");

        var risk = PredictRisk(sensor);
        log?.Invoke($"2/7 FireRiskPredictor: {risk.RiskScore:F1}% ({risk.RiskLevel}).");

        var spread = SimulateSpread(sensor, risk);
        log?.Invoke("3/7 FireSpreadSimulator: projected path generated.");

        var impact = AssessImpact(spread);
        log?.Invoke("4/7 ImpactAssessment: affected infrastructure identified.");

        var response = AdviseResponse(risk, impact);
        log?.Invoke("5/7 ResponseAdvisor: response actions generated.");

        var summary = Summarize(sensor, risk, impact, response);
        log?.Invoke("6/7 Summarizer: incident briefing generated.");

        var translations = Translate(summary);
        log?.Invoke("7/7 Translator: multilingual report placeholders generated.");

        var result = new WorkflowResult
        {
            Sensor = sensor,
            Risk = risk,
            Spread = spread,
            Impact = impact,
            Response = response,
            Summary = summary,
            Translations = translations
        };

        _repository.Save(result);
        log?.Invoke($"Record saved to {_repository.DataFile}");
        return result;
    }

    private static void ValidateSensorData(SensorData sensor)
    {
        if (string.IsNullOrWhiteSpace(sensor.RegionId))
            throw new ArgumentException("Region is required.");
        if (sensor.Humidity is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(sensor.Humidity), "Humidity must be between 0 and 100%.");
        if (sensor.WindSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(sensor.WindSpeed), "Wind speed cannot be negative.");
        if (sensor.Location.Latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(sensor.Location.Latitude));
        if (sensor.Location.Longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(sensor.Location.Longitude));
    }

    private static RiskData PredictRisk(SensorData sensor)
    {
        // Deterministic desktop replacement for the original demo's random/OpenAI score.
        var temperatureFactor = Math.Clamp((sensor.Temperature - 10) / 35.0, 0, 1) * 40;
        var drynessFactor = Math.Clamp((100 - sensor.Humidity) / 100.0, 0, 1) * 35;
        var windFactor = Math.Clamp(sensor.WindSpeed / 60.0, 0, 1) * 25;
        var score = Math.Round(Math.Clamp(temperatureFactor + drynessFactor + windFactor, 0, 100), 1);

        return new RiskData
        {
            RegionId = sensor.RegionId,
            RiskScore = score,
            RiskLevel = score switch { >= 75 => "Extreme", >= 55 => "High", >= 30 => "Moderate", _ => "Low" }
        };
    }

    private static SpreadData SimulateSpread(SensorData sensor, RiskData risk)
    {
        var distance = 0.005 + risk.RiskScore / 10_000;
        var latitude2 = sensor.Location.Latitude + distance;
        var longitude2 = sensor.Location.Longitude + distance * Math.Max(sensor.WindSpeed / 20, 0.5);
        return new SpreadData
        {
            RegionId = sensor.RegionId,
            SpreadPath = $"POINT({sensor.Location.Latitude:F5} {sensor.Location.Longitude:F5})," +
                         $"POINT({latitude2:F5} {longitude2:F5})"
        };
    }

    private static ImpactData AssessImpact(SpreadData spread)
    {
        var infrastructure = Infrastructure.TryGetValue(spread.RegionId, out var value)
            ? value
            : "Unknown Infrastructure";
        return new ImpactData
        {
            RegionId = spread.RegionId,
            ImpactedAreas = $"SpreadPath={spread.SpreadPath} | Affected={infrastructure}"
        };
    }

    private static ResponseData AdviseResponse(RiskData risk, ImpactData impact)
    {
        var action = risk.RiskLevel switch
        {
            "Extreme" => "Initiate evacuation planning; dispatch air and ground crews; establish incident command.",
            "High" => "Place response crews on standby; notify local authorities; begin public warning preparation.",
            "Moderate" => "Increase monitoring frequency; inspect access routes and available equipment.",
            _ => "Continue routine monitoring and validate the next sensor reading."
        };
        return new ResponseData
        {
            RegionId = risk.RegionId,
            Recommendations = $"{action} Review affected assets: {impact.ImpactedAreas}."
        };
    }

    private static SummaryData Summarize(
        SensorData sensor, RiskData risk, ImpactData impact, ResponseData response) => new()
    {
        RegionId = sensor.RegionId,
        Summary = $"{sensor.RegionId}: wildfire risk is {risk.RiskLevel} ({risk.RiskScore:F1}%). " +
                  $"Conditions: {sensor.Temperature:F1}°C, {sensor.Humidity:F1}% humidity, " +
                  $"{sensor.WindSpeed:F1} km/h wind. {impact.ImpactedAreas}. {response.Recommendations}"
    };

    private static IReadOnlyList<TranslationData> Translate(SummaryData summary)
    {
        // Offline legacy mode preserves the translation stage without requiring a cloud credential.
        return new[] { "es", "fr", "pt-br" }
            .Select(language => new TranslationData
            {
                RegionId = summary.RegionId,
                Language = language,
                TranslatedSummary = $"[{language.ToUpperInvariant()} translation pending operator review] {summary.Summary}"
            })
            .ToList();
    }
}
