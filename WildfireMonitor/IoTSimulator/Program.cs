
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;

class IoTSimulator
{
    private static DeviceClient deviceClient;
    // Intervalo em segundos entre mensagens
    private const int sendIntervalSeconds = 300;

    // Coloque aqui a connection string do dispositivo IoT (não do IoT Hub principal)
    private const string deviceConnectionString = "<CONECTION_STRING>";

    static async Task Main(string[] args)
    {
        deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);

        Thread.Sleep(15000);

        Console.WriteLine("Simulador IoT iniciado. Enviando dados ao IoT Hub...");

        while (true)
        {
            var telemetryData = GenerateTelemetryData();
            string messageString = JsonSerializer.Serialize(telemetryData);
            var message = new Message(Encoding.UTF8.GetBytes(messageString))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            // Envia mensagem para o IoT Hub
            await deviceClient.SendEventAsync(message);
            Console.WriteLine($"Mensagem enviada: {messageString}");

            await Task.Delay(sendIntervalSeconds * 1000);
        }
    }

    private static SensorData GenerateTelemetryData()
    {
        var random = Random.Shared;

        // Simula coordenadas GPS dentro de uma área
        double baseLat = -23.55052;    // Exemplo São Paulo latitude
        double baseLon = -46.633308;   // Exemplo São Paulo longitude

        double lat = baseLat + (random.NextDouble() - 0.5) * 0.1;
        double lon = baseLon + (random.NextDouble() - 0.5) * 0.1;

        return new SensorData
        {
            RegionId = "region-003",
            Temperature = random.Next(25, 45),    // °C
            Humidity = random.Next(10, 60),       // %
            WindSpeed = Math.Round(random.NextDouble() * 20, 2), // m/s, valor aleatório até 20
            Timestamp = DateTime.UtcNow,
            Location = new GeoLocation
            {
                Latitude = lat,
                Longitude = lon
            }
            //SmokeDetected = random.Next(0, 2) == 1, // true ou false
        };
    }

    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class SensorData
    {
        public string RegionId { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public GeoLocation Location { get; set; }
        public DateTime Timestamp { get; set; }
    }


}