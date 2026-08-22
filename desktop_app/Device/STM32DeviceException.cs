namespace desktop_app.Device;

public sealed class STM32DeviceException : Exception
{
    public STM32DeviceException(string message)
        : base(message){}

    public STM32DeviceException(string message, Exception innerException)
        : base(message, innerException){}
}