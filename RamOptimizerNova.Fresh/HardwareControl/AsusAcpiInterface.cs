using System.Runtime.InteropServices;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// ASUS ACPI interface for hardware control (like G-Helper uses)
/// This is the correct way to control ROG Flow Z13 and other ASUS laptops
/// </summary>
public class AsusAcpiInterface : IDisposable
{
    private const string FILE_NAME = @"\\.\ATKACPI";
    private const uint CONTROL_CODE = 0x0022240C;
    
    private const uint DSTS = 0x53545344; // Read device status
    private const uint DEVS = 0x53564544; // Write device value
    
    // Device IDs from G-Helper
    public const uint PerformanceMode = 0x00120075;
    public const uint GPUMuxROG = 0x00090016;
    public const uint GPUEcoROG = 0x00090020;
    public const uint BatteryLimit = 0x00120057;
    public const uint CPU_Fan = 0x00110013;
    public const uint GPU_Fan = 0x00110014;
    public const uint Temp_CPU = 0x00120094;
    public const uint Temp_GPU = 0x00120097;
    
    // P/E Core control
    public const uint CORES_CPU = 0x001200D2;
    public const uint CORES_MAX = 0x001200D3;
    
    private IntPtr _handle;
    private bool _disposed = false;
    private bool _connected = false;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        byte[] lpOutBuffer,
        uint nOutBufferSize,
        ref uint lpBytesReturned,
        IntPtr lpOverlapped
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
    private const uint FILE_SHARE_READ = 1;
    private const uint FILE_SHARE_WRITE = 2;

    public AsusAcpiInterface()
    {
        try
        {
            _handle = CreateFile(
                FILE_NAME,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero
            );

            if (_handle == IntPtr.Zero || _handle.ToInt32() == -1)
            {
                throw new InvalidOperationException("Failed to open ASUS ACPI driver. Make sure ASUS System Control Interface is installed.");
            }

            _connected = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize ASUS ACPI interface: {ex.Message}", ex);
        }
    }

    public bool IsConnected() => _connected;

    private void Control(uint dwIoControlCode, byte[] lpInBuffer, byte[] lpOutBuffer)
    {
        uint lpBytesReturned = 0;
        DeviceIoControl(
            _handle,
            dwIoControlCode,
            lpInBuffer,
            (uint)lpInBuffer.Length,
            lpOutBuffer,
            (uint)lpOutBuffer.Length,
            ref lpBytesReturned,
            IntPtr.Zero
        );
    }

    private byte[] CallMethod(uint methodId, byte[] args)
    {
        byte[] acpiBuf = new byte[8 + args.Length];
        byte[] outBuffer = new byte[16];

        BitConverter.GetBytes(methodId).CopyTo(acpiBuf, 0);
        BitConverter.GetBytes((uint)args.Length).CopyTo(acpiBuf, 4);
        Array.Copy(args, 0, acpiBuf, 8, args.Length);

        Control(CONTROL_CODE, acpiBuf, outBuffer);

        return outBuffer;
    }

    /// <summary>
    /// Read device status (DSTS method)
    /// G-Helper subtracts 65536 from the result to get the actual value
    /// </summary>
    public int DeviceGet(uint deviceId)
    {
        byte[] args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        byte[] status = CallMethod(DSTS, args);

        int rawValue = BitConverter.ToInt32(status, 0);
        
        // G-Helper subtracts 65536 to get actual value
        // This handles ASUS's return value encoding
        return rawValue - 65536;
    }

    /// <summary>
    /// Write device value (DEVS method)
    /// </summary>
    public int DeviceSet(uint deviceId, int value)
    {
        byte[] args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes(value).CopyTo(args, 4);

        byte[] status = CallMethod(DEVS, args);
        return BitConverter.ToInt32(status, 0);
    }

    /// <summary>
    /// Check if ASUS ACPI interface is available
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            var handle = CreateFile(
                FILE_NAME,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero
            );

            if (handle == IntPtr.Zero || handle.ToInt32() == -1)
                return false;

            CloseHandle(handle);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero && _handle.ToInt32() != -1)
            {
                CloseHandle(_handle);
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
