using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace desktop_app.Device;

public sealed class STM32Device : IDisposable
{
    public const int DefaultBaudRate = 115200;
    public const int DefaultTimeoutMs = 1000;

    private readonly SerialPort _serialPort;
    private readonly SemaphoreSlim _serialLock = new(1, 1);

    private static readonly Regex InfoPattern = new(
        @"^OK\s+DEVICE=\s*(?<device>\S+)\s+" +
        @"BOARD=\s*(?<board>\S+)\s+" +
        @"FW=\s*(?<fw>\S+)\s*$",
        RegexOptions.Compiled
    );

    private static readonly Regex Bme280Pattern = new(
        @"^OK\s+BME280\s+" +
        @"(?:ID=0x[0-9A-Fa-f]{2}\s+)?" +
        @"TEMP=(?<temp>-?\d+(?:\.\d+)?)C\s+" +
        @"HUM=(?<hum>\d+(?:\.\d+)?)%\s+" +
        @"PRESS=(?<press>\d+(?:\.\d+)?)hPa\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string PortName { get; }

    public int BaudRate { get; }

    public bool IsConnected => _serialPort.IsOpen;

    public STM32Device(string portName, int baudRate = DefaultBaudRate)
    {
        PortName = portName;
        BaudRate = baudRate;

        _serialPort = new SerialPort
        {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            ReadTimeout = DefaultTimeoutMs,
            WriteTimeout = DefaultTimeoutMs,
            NewLine = "\n"
        };

        Open();
    }

    public static string[] AvailablePorts()
    {
        return SerialPort.GetPortNames().OrderBy(port => port).ToArray();
    }

    private void Open()
    {
        try
        {
            _serialPort.Open();

            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
        }
        catch (Exception ex)
        {
            throw new STM32DeviceException($"Could not open {PortName}: {ex.Message}", ex);
        }
    }

    public void Close()
    {
        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        command = command.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new STM32DeviceException("Command cannot be empty.");
        }

        if (!IsConnected)
        {
            throw new STM32DeviceException("Device is not connected.");
        }

        await _serialLock.WaitAsync(cancellationToken);

        try
        {
            return await Task.Run(
                () =>
                {
                    try
                    {
                        _serialPort.DiscardInBuffer();

                        _serialPort.Write(command + "\n");

                        string response = _serialPort.ReadLine().Trim();

                        if (response.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new STM32DeviceException(response);
                        }

                        return response;
                    }
                    catch (TimeoutException)
                    {
                        throw new STM32DeviceException($"Timeout waiting for response to '{command}'.");
                    }
                    catch (STM32DeviceException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new STM32DeviceException($"Serial communication failed: {ex.Message}", ex);
                    }
                },
                cancellationToken
            );
        }
        finally
        {
            _serialLock.Release();
        }
    }

    public async Task<bool> TestAsync(CancellationToken cancellationToken = default)
    {
        string response = await SendCommandAsync("TEST", cancellationToken);

        return response == "OK TEST";
    }

    public async Task<DeviceInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        string response = await SendCommandAsync("GET INFO", cancellationToken);

        Match match = InfoPattern.Match(response);

        if (!match.Success)
        {
            throw new STM32DeviceException($"Unexpected GET INFO response: {response}");
        }

        return new DeviceInfo(
            Device: match.Groups["device"].Value,
            Board: match.Groups["board"].Value,
            Firmware: match.Groups["fw"].Value
        );
    }

    public async Task<BME280Data> GetBme280Async(CancellationToken cancellationToken = default)
    {
        string response = await SendCommandAsync("GET BME280", cancellationToken);

        Match match = Bme280Pattern.Match(response);

        if (!match.Success)
        {
            throw new STM32DeviceException($"Unexpected GET BME280 response: {response}");
        }

        double temperature = double.Parse(match.Groups["temp"].Value, CultureInfo.InvariantCulture);
        double humidity = double.Parse(match.Groups["hum"].Value, CultureInfo.InvariantCulture);
        double pressure = double.Parse(match.Groups["press"].Value, CultureInfo.InvariantCulture);

        return new BME280Data(
            Temperature: temperature,
            Humidity: humidity,
            Pressure: pressure
        );
    }

    public Task<string> SetLedAsync(bool state, CancellationToken cancellationToken = default)
    {
        string command = state ? "LED 1" : "LED 0";

        return SendCommandAsync(command, cancellationToken);
    }

    public Task<string> GetLedAsync(CancellationToken cancellationToken = default)
    {
        return SendCommandAsync("GET LED", cancellationToken);
    }

    public Task<string> GetButtonAsync(CancellationToken cancellationToken = default)
    {
        return SendCommandAsync("GET BUTTON", cancellationToken);
    }

    public void Dispose()
    {
        Close();

        _serialPort.Dispose();
        _serialLock.Dispose();
    }
}