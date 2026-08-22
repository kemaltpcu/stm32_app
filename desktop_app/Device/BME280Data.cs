namespace desktop_app.Device;

public sealed record BME280Data(
    int ChipId,
    double Temperature,
    double Humidity,
    double Pressure
);