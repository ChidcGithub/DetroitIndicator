using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace AffectionIndicator.Services;

public static class MachineIdentity
{
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "to be filled by o.e.m.",
        "default string",
        "system serial number",
        "none",
        "n/a",
        "not applicable",
        "0"
    };

    public static string GetSerialHash()
    {
        var raw = ReadBaseBoardSerial()
                  ?? ReadSystemProductUuid()
                  ?? Environment.MachineName;

        var payload = "AffectionIndicator:v1:" + raw.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? ReadBaseBoardSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT SerialNumber FROM Win32_BaseBoard");
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    var value = item["SerialNumber"]?.ToString()?.Trim();
                    if (IsUseful(value))
                        return value;
                }
            }
        }
        catch
        {
            // WMI can be unavailable on locked-down systems; use a fallback.
        }

        return null;
    }

    private static string? ReadSystemProductUuid()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT UUID FROM Win32_ComputerSystemProduct");
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    var value = item["UUID"]?.ToString()?.Trim();
                    if (IsUseful(value))
                        return value;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool IsUseful(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return !Placeholders.Contains(value.Trim());
    }
}
