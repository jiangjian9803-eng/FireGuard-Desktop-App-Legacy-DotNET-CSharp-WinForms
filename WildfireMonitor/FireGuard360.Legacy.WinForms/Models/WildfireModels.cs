namespace FireGuard360.Legacy.WinForms.Models;

public sealed class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class SensorData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public double FuelDryness { get; set; }
    public GeoLocation Location { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class RiskData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public double TemperatureContribution { get; set; }
    public double DrynessContribution { get; set; }
    public double WindContribution { get; set; }
    public double FuelContribution { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class SpreadData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public string SpreadPath { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public double EstimatedDistanceKm { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class ImpactData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public string ImpactedAreas { get; set; } = string.Empty;
    public int PopulationAtRisk { get; set; }
    public string CriticalAssets { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class ResponseData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int RequiredCrews { get; set; }
    public int RequiredEngines { get; set; }
    public int RequiredAircraft { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class SummaryData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class TranslationData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegionId { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string TranslatedSummary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class WorkflowResult
{
    public string IncidentId { get; init; } = $"WF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    public string Status { get; set; } = "Assessed";
    public required SensorData Sensor { get; init; }
    public required RiskData Risk { get; init; }
    public required SpreadData Spread { get; init; }
    public required ImpactData Impact { get; init; }
    public required ResponseData Response { get; init; }
    public required SummaryData Summary { get; init; }
    public required IReadOnlyList<TranslationData> Translations { get; init; }
    public required IReadOnlyList<AuditEvent> AuditTrail { get; init; }
}

public sealed class AuditEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Stage { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
