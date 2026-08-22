using desktop_app.Device;

namespace desktop_app;

public partial class Form1 : Form
{
    private STM32Device? _device;

    private readonly System.Windows.Forms.Timer _sensorTimer;

    private bool _pollInProgress;

    private bool _ledOn;


    public Form1()
    {
        InitializeComponent();

        _sensorTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };

        _sensorTimer.Tick += SensorTimer_Tick;

        Shown += Form1_Shown;
        FormClosing += Form1_FormClosing;

        btnRefresh.Click += BtnRefresh_Click;
        btnConnect.Click += BtnConnect_Click;

        btnGetInfo.Click += BtnGetInfo_Click;
        btnLed.Click += BtnLed_Click;
        btnGetButton.Click += BtnGetButton_Click;

        btnSend.Click += BtnSend_Click;

        txtCommand.KeyDown += TxtCommand_KeyDown;

        SetConnectedUi(false);
    }


    // ------------------------------------------------------------
    // FORM STARTUP
    // ------------------------------------------------------------

    private void Form1_Shown(object? sender, EventArgs e)
    {
        RefreshPorts();
    }


    // ------------------------------------------------------------
    // COM PORTS
    // ------------------------------------------------------------

    private void RefreshPorts()
    {
        string? previousPort = cmbPorts.SelectedItem as string;

        string[] ports = STM32Device.AvailablePorts();

        cmbPorts.Items.Clear();

        cmbPorts.Items.AddRange(ports);

        if (ports.Length == 0)
        {
            lblStatus.Text = "No COM ports found";

            btnConnect.Enabled = false;

            return;
        }

        if (previousPort != null && ports.Contains(previousPort))
        {
            cmbPorts.SelectedItem = previousPort;
        }
        else
        {
            cmbPorts.SelectedIndex = 0;
        }

        lblStatus.Text = "Disconnected";

        btnConnect.Enabled = true;
    }


    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        RefreshPorts();
    }


    // ------------------------------------------------------------
    // CONNECT / DISCONNECT
    // ------------------------------------------------------------

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_device == null)
        {
            await ConnectDeviceAsync();
        }
        else
        {
            DisconnectDevice();
        }
    }


    private async Task ConnectDeviceAsync()
    {
        if (cmbPorts.SelectedItem is not string port)
        {
            MessageBox.Show("Please select a COM port.", "No COM Port"
            );

            return;
        }

        btnConnect.Enabled = false;

        lblStatus.Text = $"Connecting to {port}...";

        STM32Device? newDevice = null;

        try
        {
            newDevice = new STM32Device(port);

            bool testPassed = await newDevice.TestAsync();

            if (!testPassed)
            {
                throw new STM32DeviceException("No Test Response!");
            }

            DeviceInfo info = await newDevice.GetInfoAsync();

            _device = newDevice;

            newDevice = null;

            UpdateDeviceInfo(info);

            SetConnectedUi(true);

            lblStatus.Text = $"Connected to {port}";

            AddResponse($"Connected successfully to {port}");

            _sensorTimer.Start();

            await ReadSensorAsync();
        }
        catch (STM32DeviceException ex)
        {
            newDevice?.Dispose();

            _device = null;

            SetConnectedUi(false);

            MessageBox.Show(ex.Message,"Connection Failed");

            AddResponse($"ERROR: {ex.Message}");
        }
        finally
        {
            btnConnect.Enabled = true;
        }
    }


    private void DisconnectDevice()
    {
        _sensorTimer.Stop();

        _device?.Dispose();

        _device = null;

        _pollInProgress = false;
        _ledOn = false;

        ResetDisplayedData();

        SetConnectedUi(false);

        lblStatus.Text = "Disconnected";

        AddResponse("Device disconnected");

        RefreshPorts();
    }


    // ------------------------------------------------------------
    // DEVICE INFORMATION
    // ------------------------------------------------------------

    private async void BtnGetInfo_Click(object? sender, EventArgs e)
    {
        if (_device == null)
        {
            return;
        }

        try
        {
            DeviceInfo info = await _device.GetInfoAsync();

            UpdateDeviceInfo(info);

            AddResponse(
                $"DEVICE={info.Device}  " +
                $"BOARD={info.Board}  " +
                $"FW={info.Firmware}");
        }
        catch (STM32DeviceException ex)
        {
            AddResponse($"ERROR: {ex.Message}");
        }
    }


    private void UpdateDeviceInfo(DeviceInfo info)
    {
        lblDeviceValue.Text = info.Device;

        lblBoardValue.Text = info.Board;

        lblFirmwareValue.Text = info.Firmware;
    }


    // ------------------------------------------------------------
    // BME280 SENSOR
    // ------------------------------------------------------------

    private async void SensorTimer_Tick(object? sender, EventArgs e)
    {
        await ReadSensorAsync();
    }


    private async Task ReadSensorAsync()
    {
        if (_device == null)
        {
            return;
        }

        if (_pollInProgress)
        {
            return;
        }

        _pollInProgress = true;

        try
        {
            BME280Data data = await _device.GetBme280Async();

            lblTemperatureValue.Text = $"{data.Temperature:F2} °C";

            lblHumidityValue.Text = $"{data.Humidity:F2} %";

            lblPressureValue.Text = $"{data.Pressure:F2} hPa";

        }
        catch (STM32DeviceException ex)
        {
            AddResponse( $"Sensor ERROR: {ex.Message}");
        }
        finally
        {
            _pollInProgress = false;
        }
    }


    // ------------------------------------------------------------
    // LED
    // ------------------------------------------------------------

    private async void BtnLed_Click(object? sender, EventArgs e)
    {
        if (_device == null)
        {
            return;
        }

        bool requestedState = !_ledOn;

        try
        {
            string response = await _device.SetLedAsync(requestedState);

            _ledOn = requestedState;

            btnLed.Text = _ledOn ? "LED OFF" : "LED ON";

            AddResponse(response);
        }

        catch (STM32DeviceException ex)
        {
            AddResponse($"ERROR: {ex.Message}");
        }
    }


    // ------------------------------------------------------------
    // BUTTON
    // ------------------------------------------------------------

    private async void BtnGetButton_Click(object? sender, EventArgs e)
    {
        if (_device == null)
        {
            return;
        }

        try
        {
            string response = await _device.GetButtonAsync();

            AddResponse(response);
        }
        catch (STM32DeviceException ex)
        {
            AddResponse($"ERROR: {ex.Message}");
        }
    }


    // ------------------------------------------------------------
    // CUSTOM COMMAND
    // ------------------------------------------------------------

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        await SendCustomCommandAsync();
    }


    private async void TxtCommand_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;

        await SendCustomCommandAsync();
    }


    private async Task SendCustomCommandAsync()
    {
        if (_device == null)
        {
            return;
        }

        string command = txtCommand.Text.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        try
        {
            AddResponse($"> {command}");

            string response = await _device.SendCommandAsync(command);

            AddResponse(response);
            txtCommand.Clear();
        }
        catch (STM32DeviceException ex)
        {
            AddResponse($"ERROR: {ex.Message}");
        }
    }


    // ------------------------------------------------------------
    // GUI STATE
    // ------------------------------------------------------------

    private void SetConnectedUi(bool connected)
    {
        cmbPorts.Enabled = !connected;

        btnRefresh.Enabled = !connected;

        btnGetInfo.Enabled = connected;

        btnLed.Enabled = connected;

        btnGetButton.Enabled = connected;

        txtCommand.Enabled = connected;

        btnSend.Enabled = connected;

        btnConnect.Text = connected ? "Disconnect" : "Connect";
    }


    private void ResetDisplayedData()
    {
        lblDeviceValue.Text = "—";

        lblBoardValue.Text = "—";

        lblFirmwareValue.Text = "—";

        lblTemperatureValue.Text = "— °C";

        lblHumidityValue.Text = "— %";

        lblPressureValue.Text = "— hPa";

        btnLed.Text = "LED ON";
    }


    // ------------------------------------------------------------
    // RESPONSE WINDOW
    // ------------------------------------------------------------

    private void AddResponse(string message)
    {
        string timestamp =  DateTime.Now.ToString("HH:mm:ss");

        txtResponse.AppendText($"[{timestamp}] {message}" +  Environment.NewLine);
        txtResponse.SelectionStart = txtResponse.Text.Length;
        txtResponse.ScrollToCaret();
    }


    // ------------------------------------------------------------
    // APPLICATION CLOSE
    // ------------------------------------------------------------

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _sensorTimer.Stop();
        _device?.Dispose();
        _device = null;
    }
}