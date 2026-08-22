namespace desktop_app;

partial class Form1
{
    private ComboBox cmbPorts = null!;

    private Button btnRefresh = null!;
    private Button btnConnect = null!;

    private Label lblStatus = null!;

    private Label lblDeviceValue = null!;
    private Label lblBoardValue = null!;
    private Label lblFirmwareValue = null!;

    private Label lblTemperatureValue = null!;
    private Label lblHumidityValue = null!;
    private Label lblPressureValue = null!;

    private Button btnGetInfo = null!;
    private Button btnLed = null!;
    private Button btnGetButton = null!;

    private TextBox txtCommand = null!;
    private Button btnSend = null!;

    private TextBox txtResponse = null!;


    private void InitializeComponent()
    {
        Text = "STM32 Device Manager";

        ClientSize = new Size(900, 900);

        StartPosition = FormStartPosition.CenterScreen;

        MinimumSize = new Size(850, 650);


        // ========================================================
        // CONNECTION
        // ========================================================

        GroupBox connectionGroup =
            new GroupBox
            {
                Text = "Connection",
                Location = new Point(20, 20),
                Size = new Size(850, 80)
            };


        Label portLabel =
            new Label
            {
                Text = "COM Port:",
                Location = new Point(20, 35),
                AutoSize = true
            };


        cmbPorts =
            new ComboBox
            {
                Location = new Point(90, 30),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };


        btnRefresh =
            new Button
            {
                Text = "Refresh",
                Location = new Point(245, 29),
                Size = new Size(90, 30)
            };


        btnConnect =
            new Button
            {
                Text = "Connect",
                Location = new Point(345, 29),
                Size = new Size(100, 30)
            };


        Label statusTitle =
            new Label
            {
                Text = "Status:",
                Location = new Point(480, 35),
                AutoSize = true
            };


        lblStatus = new Label
        {
            Text = "Disconnected",
            Location = new Point(535, 35),
            Size = new Size(280, 25)
        };


        connectionGroup.Controls.Add(portLabel);
        connectionGroup.Controls.Add(cmbPorts);
        connectionGroup.Controls.Add(btnRefresh);
        connectionGroup.Controls.Add(btnConnect);
        connectionGroup.Controls.Add(statusTitle);
        connectionGroup.Controls.Add(lblStatus);

        // ========================================================
        // DEVICE INFORMATION
        // ========================================================

        GroupBox deviceGroup = new GroupBox
        {
            Text = "Device Information",
            Location = new Point(20, 115),
            Size = new Size(850, 90)
        };


        Label deviceTitle = new Label
        {
            Text = "Device",
            Location = new Point(25, 25),
            AutoSize = true
        };


        lblDeviceValue = new Label
        {
            Text = "—",
            Location = new Point(25, 50),
            Size = new Size(230, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };


        Label boardTitle = new Label
        {
            Text = "Board",
            Location = new Point(300, 25),
            AutoSize = true
        };


        lblBoardValue = new Label
        {
            Text = "—",
            Location = new Point(300, 50),
            Size = new Size(230, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };


        Label firmwareTitle = new Label
        {
            Text = "Firmware",
            Location = new Point(600, 25),
            AutoSize = true
        };


        lblFirmwareValue = new Label
        {
            Text = "—",
            Location = new Point(600, 50),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold
            )
        };

        deviceGroup.Controls.Add(deviceTitle);
        deviceGroup.Controls.Add(lblDeviceValue);
        deviceGroup.Controls.Add(boardTitle );
        deviceGroup.Controls.Add(lblBoardValue);
        deviceGroup.Controls.Add(firmwareTitle);
        deviceGroup.Controls.Add(lblFirmwareValue);


        // ========================================================
        // SENSOR DATA
        // ========================================================

        GroupBox sensorGroup = new GroupBox
        {
            Text = "BME280 Environment",
            Location = new Point(20, 220),
            Size = new Size(850, 130)
        };


        Label temperatureTitle = new Label
        {
            Text = "Temperature",
            Location = new Point(30, 30),
            AutoSize = true
        };


        lblTemperatureValue = new Label
            {
                Text = "— °C",
                Location = new Point(30, 60),
                Size = new Size(170, 40),
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };


        Label humidityTitle = new Label
        {
            Text = "Humidity",
            Location = new Point(240, 30),
            AutoSize = true
        };


        lblHumidityValue = new Label
            {
                Text = "— %",
                Location = new Point(240, 60),
                Size = new Size(170, 40),
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };


        Label pressureTitle = new Label
        {
            Text = "Pressure",
            Location = new Point(450, 30),
            AutoSize = true
        };


        lblPressureValue = new Label
        {
            Text = "— hPa",
            Location = new Point(450, 60),
            Size = new Size(190, 40),
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };

        sensorGroup.Controls.Add(temperatureTitle);
        sensorGroup.Controls.Add(lblTemperatureValue);
        sensorGroup.Controls.Add(humidityTitle);
        sensorGroup.Controls.Add(lblHumidityValue);
        sensorGroup.Controls.Add(pressureTitle);
        sensorGroup.Controls.Add(lblPressureValue);


        // ========================================================
        // DEVICE CONTROLS
        // ========================================================

        GroupBox controlGroup = new GroupBox
        {
            Text = "Device Controls",
            Location = new Point(20, 365),
            Size = new Size(850, 75)
        };


        btnGetInfo = new Button
        {
            Text = "GET INFO",
            Location = new Point(25, 28),
            Size = new Size(120, 30)
        };


        btnLed = new Button
        {
            Text = "LED ON",
            Location = new Point(160, 28),
            Size = new Size(120, 30)
        };


        btnGetButton = new Button
        {
            Text = "GET BUTTON",
            Location = new Point(295, 28),
            Size = new Size(120, 30)
        };


        controlGroup.Controls.Add(btnGetInfo );

        controlGroup.Controls.Add( btnLed);

        controlGroup.Controls.Add(btnGetButton);


        // ========================================================
        // CUSTOM COMMAND
        // ========================================================

        GroupBox commandGroup = new GroupBox
        {
            Text = "Send Message / Command",
            Location = new Point(20, 455),
            Size = new Size(850, 80)
        };


        txtCommand = new TextBox
        {
            Location = new Point(25, 30),
            Size = new Size(680, 27)
        };


        btnSend = new Button
        {
            Text = "Send",
            Location = new Point(720, 28),
            Size = new Size(100, 30)
        };


        commandGroup.Controls.Add(txtCommand);

        commandGroup.Controls.Add(btnSend);


        // ========================================================
        // RESPONSE / LOG
        // ========================================================

        GroupBox responseGroup = new GroupBox
        {
            Text = "Device Response / Log",
            Location = new Point(20, 550),
            Size = new Size(850, 125)
        };


        txtResponse = new TextBox
            {
                Location = new Point(15, 25),
                Size = new Size(820, 85),

                Multiline = true,
                ReadOnly = true,

                ScrollBars = ScrollBars.Vertical,

                Font = new Font("Consolas", 10)
            };


        responseGroup.Controls.Add(txtResponse);


        // ========================================================
        // FORM
        // ========================================================

        Controls.Add( connectionGroup);

        Controls.Add(deviceGroup);

        Controls.Add(sensorGroup);

        Controls.Add(controlGroup);

        Controls.Add(commandGroup);

        Controls.Add(responseGroup);
    }
}
