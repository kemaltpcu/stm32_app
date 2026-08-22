using desktop_app.Device;

namespace desktop_app;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        Shown += Form1_Shown;
    }

    private void Form1_Shown(
        object? sender,
        EventArgs e)
    {
        string[] ports =
            STM32Device.AvailablePorts();

        if (ports.Length == 0)
        {
            MessageBox.Show(
                "No COM ports were found.",
                "COM Ports"
            );

            return;
        }

        string message =
            "Available COM ports:\n\n" +
            string.Join("\n", ports);

        MessageBox.Show(
            message,
            "COM Ports"
        );
    }
}