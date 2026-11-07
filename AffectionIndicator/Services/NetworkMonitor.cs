using System.Net.NetworkInformation;

namespace AffectionIndicator.Services;

public sealed class NetworkMonitor : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private bool _isOnline;

    public bool IsOnline => _isOnline;

    public event EventHandler<bool>? StatusChanged;

    public NetworkMonitor()
    {
        _isOnline = Probe();
        NetworkChange.NetworkAvailabilityChanged += OnAvailabilityChanged;
        NetworkChange.NetworkAddressChanged += OnAddressChanged;

        _timer = new System.Timers.Timer(5000) { AutoReset = true };
        _timer.Elapsed += (_, _) => Refresh();
        _timer.Start();
    }

    private static bool Probe()
    {
        try
        {
            return NetworkInterface.GetIsNetworkAvailable() &&
                   NetworkInterface.GetAllNetworkInterfaces().Any(n =>
                       n.OperationalStatus == OperationalStatus.Up &&
                       n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        }
        catch
        {
            return false;
        }
    }

    private void OnAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) => Refresh();

    private void OnAddressChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var online = Probe();
        if (online == _isOnline)
            return;

        _isOnline = online;
        StatusChanged?.Invoke(this, online);
    }

    public void Recheck() => Refresh();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        NetworkChange.NetworkAvailabilityChanged -= OnAvailabilityChanged;
        NetworkChange.NetworkAddressChanged -= OnAddressChanged;
    }
}
