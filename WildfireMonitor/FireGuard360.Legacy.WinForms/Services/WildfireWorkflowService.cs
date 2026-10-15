using FireGuard360.Legacy.WinForms.Data;
using FireGuard360.Legacy.WinForms.Models;

namespace FireGuard360.Legacy.WinForms.Services;

public sealed class WildfireWorkflowService
{
    private sealed record RegionProfile(string Infrastructure, int Population, double Vulnerability);

    private static readonly IReadOnlyDictionary<string, RegionProfile> Regions =
        new Dictionary<string, RegionProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["region-001"] = new("Hospital, Power Plant", 2400, 1.20),
            ["region-002"] = new("School, Highway", 1250, 1.05),
            ["region-003"] = new("Farm, Water Reservoir", 680, 0.90)
        };

    private readonly LocalWildfireRepository _repository;

    public WildfireWorkflowService(LocalWildfireRepository repository) => _repository = repository;

    public WorkflowResult Execute(SensorData sensor, Action<string>? log = null)
    {
        var audit = new List<AuditEvent>();
        ValidateSensorData(sensor);
        Record(audit, log, "Data Collection", "Sensor reading validated and normalized.");

        var risk = PredictRisk(sensor);
        Record(audit, log, "Risk Prediction", $"Risk {risk.RiskScore:F1}% ({risk.RiskLevel}); {risk.Explanation}");

        var spread = SimulateSpread(sensor, risk);
        Record(audit, log, "Spread Simulation", $"Projected {spread.EstimatedDistanceKm:F1} km toward {spread.Direction}.");

        var impact = AssessImpact(spread, risk);
        Record(audit, log, "Impact Assessment", $"Estimated population at risk: {impact.PopulationAtRisk:N0}; assets: {impact.CriticalAssets}.");

        var response = AdviseResponse(risk, impact);
        Record(audit, log, "Response Planning", $"Priority {response.Priority}; resources calculated.");

        var summary = Summarize(sensor, risk, impact, response);
        Record(audit, log, "Briefing", "Executive incident summary generated.");

        var translations = Translate(summary);
        Record(audit, log, "Translation", "French, Spanish, and Portuguese review records prepared.");

        var result = new WorkflowResult
        {
            Sensor = sensor,
            Risk = risk,
            Spread = spread,
            Impact = impact,
            Response = response,
            Summary = summary,
            Translations = translations,
            AuditTrail = audit
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
        if (sensor.WindDirection is < 0 or > 359)
            throw new ArgumentOutOfRangeException(nameof(sensor.WindDirection), "Wind direction must be between 0 and 359 degrees.");
        if (sensor.FuelDryness is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(sensor.FuelDryness), "Fuel dryness must be between 0 and 100%.");
    }

    private static RiskData PredictRisk(SensorData sensor)
    {
        // Deterministic desktop replacement for the original demo's random/OpenAI score.
        var temperatureFactor = Math.Clamp((sensor.Temperature - 10) / 35.0, 0, 1) * 30;
        var drynessFactor = Math.Clamp((100 - sensor.Humidity) / 100.0, 0, 1) * 25;
        var windFactor = Math.Clamp(sensor.WindSpeed / 60.0, 0, 1) * 20;
        var fuelFactor = Math.Clamp(sensor.FuelDryness / 100.0, 0, 1) * 25;
        var score = Math.Round(Math.Clamp(temperatureFactor + drynessFactor + windFactor + fuelFactor, 0, 100), 1);

        return new RiskData
        {
            RegionId = sensor.RegionId,
            RiskScore = score,
            RiskLevel = score switch { >= 75 => "Extreme", >= 55 => "High", >= 30 => "Moderate", _ => "Low" },
            TemperatureContribution = Math.Round(temperatureFactor, 1),
            DrynessContribution = Math.Round(drynessFactor, 1),
            WindContribution = Math.Round(windFactor, 1),
            FuelContribution = Math.Round(fuelFactor, 1),
            Explanation = $"temperature +{temperatureFactor:F1}, humidity +{drynessFactor:F1}, " +
                          $"wind +{windFactor:F1}, fuel +{fuelFactor:F1}"
        };
    }

    private static SpreadData SimulateSpread(SensorData sensor, RiskData risk)
    {
        var estimatedDistance = Math.Round(0.5 + risk.RiskScore / 18 + sensor.WindSpeed / 22, 1);
        var radians = sensor.WindDirection * Math.PI / 180;
        var latitude2 = sensor.Location.Latitude + Math.Cos(radians) * estimatedDistance / 111.0;
        var longitudeScale = Math.Max(Math.Cos(sensor.Location.Latitude * Math.PI / 180), 0.2);
        var longitude2 = sensor.Location.Longitude + Math.Sin(radians) * estimatedDistance / (111.0 * longitudeScale);
        return new SpreadData
        {
            RegionId = sensor.RegionId,
            SpreadPath = $"POINT({sensor.Location.Latitude:F5} {sensor.Location.Longitude:F5})," +
                         $"POINT({latitude2:F5} {longitude2:F5})",
            Direction = ToCompassDirection(sensor.WindDirection),
            EstimatedDistanceKm = estimatedDistance
        };
    }

    private static ImpactData AssessImpact(SpreadData spread, RiskData risk)
    {
        var profile = Regions.TryGetValue(spread.RegionId, out var value)
            ? value
            : new RegionProfile("Unknown Infrastructure", 300, 1.0);
        var exposureRatio = Math.Clamp((risk.RiskScore - 20) / 80.0, 0.05, 1.0);
        var population = (int)Math.Round(profile.Population * exposureRatio * profile.Vulnerability);
        return new ImpactData
        {
            RegionId = spread.RegionId,
            ImpactedAreas = $"Projected corridor {spread.Direction}, {spread.EstimatedDistanceKm:F1} km",
            PopulationAtRisk = population,
            CriticalAssets = profile.Infrastructure
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
            Priority = risk.RiskLevel switch { "Extreme" => "P1 - Immediate", "High" => "P2 - Urgent", "Moderate" => "P3 - Elevated", _ => "P4 - Routine" },
            RequiredCrews = risk.RiskLevel switch { "Extreme" => 8, "High" => 5, "Moderate" => 3, _ => 1 },
            RequiredEngines = risk.RiskLevel switch { "Extreme" => 6, "High" => 4, "Moderate" => 2, _ => 1 },
            RequiredAircraft = risk.RiskLevel switch { "Extreme" => 3, "High" => 2, "Moderate" => 1, _ => 0 },
            Recommendations = $"{action} Protect {impact.CriticalAssets}; estimated population at risk: {impact.PopulationAtRisk:N0}."
        };
    }

    private static SummaryData Summarize(
        SensorData sensor, RiskData risk, ImpactData impact, ResponseData response) => new()
    {
        RegionId = sensor.RegionId,
        Summary = $"{sensor.RegionId}: wildfire risk is {risk.RiskLevel} ({risk.RiskScore:F1}%). " +
                  $"Conditions: {sensor.Temperature:F1}°C, {sensor.Humidity:F1}% humidity, " +
                  $"{sensor.WindSpeed:F1} km/h wind, {sensor.FuelDryness:F0}% fuel dryness. " +
                  $"{impact.ImpactedAreas}; affected assets: {impact.CriticalAssets}. {response.Recommendations}"
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

    private static void Record(List<AuditEvent> audit, Action<string>? log, string stage, string detail)
    {
        audit.Add(new AuditEvent { Stage = stage, Detail = detail });
        log?.Invoke($"{stage}: {detail}");
    }

    private static string ToCompassDirection(int degrees)
    {
        string[] directions = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        return directions[(int)Math.Round(degrees / 45.0, MidpointRounding.AwayFromZero) % 8];
    }
}
