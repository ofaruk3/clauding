using System.Runtime.InteropServices;

namespace Clauding;

static partial class N
{
    [DllImport("kernel32.dll")] public static extern ushort GetUserDefaultUILanguage();
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
    public static bool Basili(Keys k) => (GetAsyncKeyState((int)k) & 0x8000) != 0;

    // ---- device list (macro pad detection) ----
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern IntPtr SetupDiGetClassDevs(IntPtr cls, string? enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll")] static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint i, ref SP_DEVINFO_DATA d);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern bool SetupDiGetDeviceInstanceId(IntPtr set, ref SP_DEVINFO_DATA d, System.Text.StringBuilder id, int size, out int gerekli);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [StructLayout(LayoutKind.Sequential)] struct SP_DEVINFO_DATA { public uint cbSize; public Guid ClassGuid; public uint DevInst; public IntPtr Reserved; }

    /// <summary>Instance ids of every present device on the given bus ("HID", "USB").</summary>
    public static List<string> Aygitlar(string veriyolu)
    {
        var sonuc = new List<string>();
        var set = SetupDiGetClassDevs(IntPtr.Zero, veriyolu, IntPtr.Zero, 0x2 /*PRESENT*/ | 0x4 /*ALLCLASSES*/);
        if (set == new IntPtr(-1)) return sonuc;
        try
        {
            var d = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
            var sb = new System.Text.StringBuilder(512);
            for (uint i = 0; SetupDiEnumDeviceInfo(set, i, ref d); i++)
                if (SetupDiGetDeviceInstanceId(set, ref d, sb, sb.Capacity, out _)) sonuc.Add(sb.ToString());
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return sonuc;
    }
}

/// <summary>What is plugged in / available.</summary>
static class Donanim
{
    /// <summary>CH57x-based 3×4 + 2 knob macro pads (VID 1189, PID 8840/8842/8850) — the ones ch57x-keyboard-tool can program.</summary>
    public static bool MakroKlavyeVar()
    {
        try { return N.Aygitlar("USB").Any(id => id.Contains("VID_1189&PID_88", StringComparison.OrdinalIgnoreCase)); }
        catch { return false; }
    }

    public static int EkranSayisi => Screen.AllScreens.Length;

    /// <summary>Windows display language: Turkish → "tr", anything else → "en".</summary>
    public static string SistemDili => (N.GetUserDefaultUILanguage() & 0x3FF) == 0x1F ? "tr" : "en";
}
