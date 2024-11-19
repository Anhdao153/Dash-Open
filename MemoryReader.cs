using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public class MemoryReader
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    private const int PROCESS_WM_READ = 0x0010;

    public int ReadInt(Process process, IntPtr address)
    {
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        byte[] buffer = new byte[sizeof(int)];
        IntPtr bytesRead;

        try
        {
            if (ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead) && bytesRead.ToInt32() == buffer.Length)
            {
                return BitConverter.ToInt32(buffer, 0);
            }
            throw new Exception("Failed to read int from memory.");
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    private const int DefaultBufferSize = 256;

    public string ReadString(Process process, IntPtr address, int maxLength = DefaultBufferSize)
    {
        byte[] buffer = new byte[maxLength];
        int bytesRead;

        if (ReadProcessMemory(process.Handle, address, buffer, maxLength, out bytesRead) && bytesRead > 0)
        {
            return Encoding.ASCII.GetString(buffer).Split('\0')[0];
        }
        else
        {
            return string.Empty;
        }
    }

    public float ReadFloat(Process process, IntPtr address)
    {
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        byte[] buffer = new byte[sizeof(float)];
        IntPtr bytesRead;

        try
        {
            if (ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead) && bytesRead.ToInt32() == buffer.Length)
            {
                return BitConverter.ToSingle(buffer, 0);
            }
            throw new Exception("Failed to read float from memory.");
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    public long ReadInt64(Process process, IntPtr address)
    {
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        byte[] buffer = new byte[sizeof(long)];
        IntPtr bytesRead;

        try
        {
            if (ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead) && bytesRead.ToInt32() == buffer.Length)
            {
                return BitConverter.ToInt64(buffer, 0);
            }
            throw new Exception("Failed to read long from memory.");
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    public IntPtr ReadIntPtr(Process process, IntPtr address)
    {
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        byte[] buffer = new byte[IntPtr.Size];
        IntPtr bytesRead;

        try
        {
            if (ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead) && bytesRead.ToInt32() == buffer.Length)
            {
                return IntPtr.Size == 4 ? new IntPtr(BitConverter.ToInt32(buffer, 0)) : new IntPtr(BitConverter.ToInt64(buffer, 0));
            }
            throw new Exception("Failed to read IntPtr from memory.");
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    public short ReadInt16(Process process, IntPtr address)
    {
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        byte[] buffer = new byte[sizeof(short)];
        IntPtr bytesRead;

        try
        {
            if (ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead) && bytesRead.ToInt32() == buffer.Length)
            {
                return BitConverter.ToInt16(buffer, 0);
            }
            throw new Exception("Failed to read short from memory.");
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    public byte ReadByte(Process process, IntPtr address)
    {
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        byte[] buffer = new byte[1];
        IntPtr bytesRead;

        try
        {
            if (ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead) && bytesRead.ToInt32() == buffer.Length)
            {
                return buffer[0];
            }
            throw new Exception("Failed to read byte from memory.");
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    public string ReadVIPFromMemory(Process process, IntPtr vipAddress)
    {
        try
        {
            byte[] buffer = new byte[256];
            IntPtr bytesRead;

            bool success = ReadProcessMemory(process.Handle, vipAddress, buffer, buffer.Length, out bytesRead);
            if (success)
            {
                int nullTerminatorIndex = Array.IndexOf(buffer, (byte)0);
                if (nullTerminatorIndex == -1) nullTerminatorIndex = buffer.Length;
                return Encoding.UTF8.GetString(buffer, 0, nullTerminatorIndex);
            }
            else
            {
                throw new Exception("Failed to read VIP address from memory.");
            }
        }
        catch (Exception)
        {
            return "N/A";
        }
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_SHIFT = 0x10;
    private const byte VK_3 = 0x33;

    private void SendShiftAnd3()
    {
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(VK_3, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(VK_3, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
    }
}
