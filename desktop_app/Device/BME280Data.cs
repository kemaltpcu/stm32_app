namespace desktop_app.Device;

public sealed record BME280Data(
    double Temperature,
    double Humidity,
    double Pressure
);