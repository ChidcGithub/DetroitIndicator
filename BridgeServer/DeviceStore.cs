using System.Security.Cryptography;
using System.Text.Json;

namespace BridgeServer;

public sealed class DeviceStore
{
    private const string IdAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;
    private readonly object _gate = new();
    private List<DeviceRecord> _devices;

    public DeviceStore(string path)
    {
        _path = path;
        _devices = Load();
    }

    public DeviceRecord Register(string serialHash, string? machineName, out bool isNew)
    {
        lock (_gate)
        {
            var existing = _devices.FirstOrDefault(d =>
                string.Equals(d.SerialHash, serialHash, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(machineName))
                    existing.MachineName = machineName.Trim();
                isNew = false;
                SaveLocked();
                return existing;
            }

            string id;
            do
            {
                id = GenerateId();
            } while (_devices.Any(d => d.DeviceId == id));

            var device = new DeviceRecord
            {
                DeviceId = id,
                SerialHash = serialHash,
                Token = GenerateToken(),
                MachineName = string.IsNullOrWhiteSpace(machineName) ? "Windows PC" : machineName.Trim()
            };
            _devices.Add(device);
            SaveLocked();
            isNew = true;
            return device;
        }
    }

    public bool TryGet(string deviceId, out DeviceRecord device)
    {
        lock (_gate)
        {
            device = _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))!;
            return device != null;
        }
    }

    public bool Validate(string deviceId, string token)
    {
        lock (_gate)
        {
            var device = _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
            return device != null && FixedTimeEquals(device.Token, token);
        }
    }

    public void Touch(string deviceId)
    {
        lock (_gate)
        {
            var device = _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
            if (device == null)
                return;

            device.LastSeenUtc = DateTimeOffset.UtcNow;
            SaveLocked();
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
                return _devices.Count;
        }
    }

    private List<DeviceRecord> Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<List<DeviceRecord>>(json, JsonOptions) ?? new();
            }
        }
        catch
        {
            // A broken data file should not prevent the bridge from starting.
        }
        return new();
    }

    private void SaveLocked()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_devices, JsonOptions));
            File.Move(tmp, _path, overwrite: true);
        }
        catch
        {
            // The in-memory store remains usable if the data cannot be saved.
        }
    }

    private static string GenerateId()
    {
        Span<char> chars = stackalloc char[10];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = IdAlphabet[RandomNumberGenerator.GetInt32(IdAlphabet.Length)];
        return new string(chars);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
