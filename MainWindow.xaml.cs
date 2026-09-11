using System.Windows;
using System.Windows.Media;
using NAudio.CoreAudioApi;
using DigiRigControlCenter.Services;

namespace DigiRigControlCenter;

public partial class MainWindow : Window
{
    private readonly AudioService audio = new();
    private readonly Cm108PttService ptt = new();
    private MMDevice? rx;
    private MMDevice? tx;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        Closing += (_, _) => { audio.Dispose(); };
        audio.InputLevelChanged += Audio_InputLevelChanged;
        audio.OutputStereoLevelChanged += Audio_OutputStereoLevelChanged;
        audio.MonitorError += Audio_MonitorError;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        try
        {
            var inputs = audio.GetInputs();
            var outputs = audio.GetOutputs();
            InputsList.ItemsSource = inputs.Select(x => $"{(x.IsDigiRigCandidate ? "● DIGIRIG  " : "    ")}{x.Name}   {x.SampleFormat}   {(x.IsDefault ? "[DEFAULT]" : "")}");
            OutputsList.ItemsSource = outputs.Select(x => $"{(x.IsDigiRigCandidate ? "● DIGIRIG  " : "    ")}{x.Name}   {x.SampleFormat}   {(x.IsDefault ? "[DEFAULT]" : "")}");

            rx = audio.FindDigiRigInput();
            tx = audio.FindDigiRigOutput();
            if (tx != null)
                audio.EnsureOutputMeter(tx);
            else
                audio.StopOutputMeter();
            bool found = rx != null && tx != null;
            DeviceStatus.Text = found ? "● DigiRig Lite detected" : "✗ DigiRig Lite not detected";
            DeviceStatus.Foreground = found ? (Brush)FindResource("GoodBrush") : (Brush)FindResource("BadBrush");
            ptt.Refresh();
            PttDeviceText.Text = "PTT HID: " + ptt.DeviceDescription;

            RxText.Text = rx == null ? "Not found" : rx.FriendlyName;
            TxText.Text = tx == null ? "Not found" : tx.FriendlyName;
            EnhText.Text = (rx != null && GetEnhancementState(rx)) && (tx != null && GetEnhancementState(tx)) ? "✓ Disabled" : "⚠ Check / fix";
            EnhText.Foreground = EnhText.Text.StartsWith("✓") ? (Brush)FindResource("GoodBrush") : (Brush)FindResource("WarnBrush");
            AgcText.Text = "Driver control — use Custom tab";
            AgcText.Foreground = (Brush)FindResource("WarnBrush");
            if (rx != null) InputLevelSlider.Value = audio.GetInputVolume() * 100.0;
            if (tx != null) OutputLevelSlider.Value = audio.GetOutputVolume() * 100.0;
            AudioMessage.Text = found ? "DigiRig audio endpoints are ready. Set RX input and TX output levels below. The meter shows live RX audio." : "Connect the DigiRig Lite and press Refresh.";
        }
        catch (Exception ex)
        {
            AudioMessage.Text = "Refresh error: " + ex.Message;
        }
    }

    private bool GetEnhancementState(MMDevice d)
    {
        // Re-read by comparing the endpoint's property through the public service data.
        return audio.GetInputs().Concat(audio.GetOutputs()).FirstOrDefault(x => x.Id == d.ID)?.EnhancementsDisabled ?? false;
    }

    private void FixEnhancements_Click(object sender, RoutedEventArgs e)
    {
        int ok = 0;
        if (rx != null && audio.TryDisableEnhancements(rx, out var rmsg)) ok++;
        if (tx != null && audio.TryDisableEnhancements(tx, out var tmsg)) ok++;
        AudioMessage.Text = ok > 0
            ? "Enhancement/system-effects disable request applied. Refresh to verify. AGC is driver-specific and is handled from the native Custom tab."
            : "Windows did not expose the endpoint effects control. Use the native Windows audio properties to disable enhancements.";
        Refresh();
    }

    private void OpenProperties_Click(object sender, RoutedEventArgs e) => audio.OpenLegacySoundProperties();

    private void FixAll_Click(object sender, RoutedEventArgs e)
    {
        FixEnhancements_Click(sender, e);
        audio.OpenLegacySoundProperties();
    }

    private void RenameDigiRig_Click(object sender, RoutedEventArgs e)
    {
        if (audio.TryRenameDigiRigDevices(out var message))
        {
            AudioMessage.Text = message + " Refreshing device list…";
        }
        else
        {
            AudioMessage.Text = message;
        }
        Refresh();
    }

    private void InputLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // During InitializeComponent(), WPF can raise ValueChanged before all
        // x:Name fields have been assigned. Do not touch the value label until
        // the XAML object tree is fully constructed.
        if (InputLevelValue != null)
            InputLevelValue.Text = $"{e.NewValue:0}%";

        if (IsLoaded && rx != null && Math.Abs(e.NewValue - audio.GetInputVolume() * 100) > 0.5)
            audio.TrySetInputVolume((float)(e.NewValue / 100.0), out _);
    }

    private void OutputLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // During InitializeComponent(), WPF can raise ValueChanged before all
        // x:Name fields have been assigned. Do not touch the value label until
        // the XAML object tree is fully constructed.
        if (OutputLevelValue != null)
            OutputLevelValue.Text = $"{e.NewValue:0}%";

        if (IsLoaded && tx != null && Math.Abs(e.NewValue - audio.GetOutputVolume() * 100) > 0.5)
            audio.TrySetOutputVolume((float)(e.NewValue / 100.0), out _);
    }

    private void Audio_InputLevelChanged(float level)
    {
        Dispatcher.BeginInvoke(() => InputMeter.Value = level);
    }

    private void Audio_OutputStereoLevelChanged(float left, float right)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (OutputLeftMeter != null) OutputLeftMeter.Value = left;
            if (OutputRightMeter != null) OutputRightMeter.Value = right;
            if (TxAudioStatus != null)
                TxAudioStatus.Text = left < 0.5 && right < 0.5
                    ? "TX audio: idle"
                    : $"TX audio: LEFT {left:0}%  |  RIGHT {right:0}%";

            if (VoxStatus != null)
            {
                VoxStatus.Text = right >= 5.0f
                    ? "● Right-channel VOX: audio present (may key PTT)"
                    : "Right-channel VOX: inactive";
                VoxStatus.Foreground = right >= 5.0f
                    ? (Brush)FindResource("BadBrush")
                    : (Brush)FindResource("MutedBrush");
            }
        });
    }

    private void Audio_MonitorError(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            MonitorStatus.Text = "Monitor error: " + message;
            MonitorStatus.Foreground = (Brush)FindResource("BadBrush");
        });
    }

    private void StartMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (rx == null) { MonitorStatus.Text = "DigiRig RX not found."; return; }
        try
        {
            using var deviceEnumerator = new MMDeviceEnumerator();
            var defaultOut = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            audio.StartMonitor(rx, defaultOut, tx);
            MonitorStatus.Text = tx != null
                ? $"● Monitoring {rx.FriendlyName} → {defaultOut.FriendlyName}  |  TX meter: {tx.FriendlyName}"
                : $"● Monitoring {rx.FriendlyName} → {defaultOut.FriendlyName}  |  DigiRig Out not found";
            MonitorStatus.Foreground = (Brush)FindResource("GoodBrush");
        }
        catch (Exception ex) { MonitorStatus.Text = "Monitor error: " + ex.Message; }
    }

    private void StopMonitor_Click(object sender, RoutedEventArgs e)
    {
        audio.StopMonitor();
        MonitorStatus.Text = "Stopped";
        MonitorStatus.Foreground = (Brush)FindResource("MutedBrush");
    }

    private void TestTone_Click(object sender, RoutedEventArgs e)
    {
        if (tx == null)
        {
            TestToneStatus.Text = "DigiRig Out not found.";
            TestToneStatus.Foreground = (Brush)FindResource("BadBrush");
            return;
        }

        try
        {
            if (audio.IsTestToneRunning)
            {
                audio.StopTestTone();
                TestToneButton.Content = "TEST TONE";
                TestToneStatus.Text = "Test tone off";
                TestToneStatus.Foreground = (Brush)FindResource("MutedBrush");
            }
            else
            {
                audio.StartTestTone(tx);
                TestToneButton.Content = "STOP TONE";
                TestToneStatus.Text = "● 1 kHz test tone active";
                TestToneStatus.Foreground = (Brush)FindResource("GoodBrush");
            }
        }
        catch (Exception ex)
        {
            TestToneStatus.Text = "Test tone error: " + ex.Message;
            TestToneStatus.Foreground = (Brush)FindResource("BadBrush");
        }
    }

    private async void TestPtt_Click(object sender, RoutedEventArgs e)
    {
        if (!ptt.Refresh()) { PttStatus.Text = "DigiRig Lite HID PTT not detected."; return; }
        if (!double.TryParse(PttSeconds.Text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
            seconds = 3.0;
        seconds = Math.Clamp(seconds, 0.1, 10.0);
        TestPttButton.IsEnabled = false;
        PttStatus.Text = "● PTT ON — radio TX asserted by C-Media HID";
        PttStatus.Foreground = (Brush)FindResource("BadBrush");
        string err = "";
        bool success = await Task.Run(() => ptt.TestPtt(TimeSpan.FromSeconds(seconds), out err));
        if (success)
        {
            PttStatus.Text = "PTT OFF — radio TX released";
            PttStatus.Foreground = (Brush)FindResource("GoodBrush");
        }
        else
        {
            PttStatus.Text = string.IsNullOrWhiteSpace(err)
                ? "PTT error — check connection"
                : "PTT error: " + err;
            PttStatus.Foreground = (Brush)FindResource("BadBrush");
        }
        TestPttButton.IsEnabled = true;
    }
}
