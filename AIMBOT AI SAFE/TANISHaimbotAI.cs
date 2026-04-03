using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TANISHregedit
{
    public class TANISHaimbotAI
    {
        #region WinAPI

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            ProcessAccessFlags dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            IntPtr nSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            IntPtr nSize,
            IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer,
            uint dwLength);

        #endregion

        public IntPtr ProcessHandle;
        public int ProcessId;

        #region STRUCTS

        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        struct PatternData
        {
            public byte[] Pattern;
            public byte[] Mask;
        }

        #endregion

        #region PROCESS

        public bool SetProcess(string processName)
        {
            var proc = Process.GetProcessesByName(processName).FirstOrDefault();
            if (proc == null) return false;

            ProcessId = proc.Id;
            ProcessHandle = OpenProcess(ProcessAccessFlags.AllAccess, false, ProcessId);
            return ProcessHandle != IntPtr.Zero;
        }

        #endregion

        #region READ / WRITE

        public byte[] ReadBytes(long address, int length)
        {
            byte[] buffer = new byte[length];
            ReadProcessMemory(
                ProcessHandle,
                (IntPtr)address,
                buffer,
                (IntPtr)length,
                out _);
            return buffer;
        }

        public bool WriteBytes(long address, byte[] bytes)
        {
            return WriteProcessMemory(
                ProcessHandle,
                (IntPtr)address,
                bytes,
                (IntPtr)bytes.Length,
                IntPtr.Zero);
        }

        public int ReadInt(long address)
        {
            return BitConverter.ToInt32(ReadBytes(address, 4), 0);
        }

        #endregion

        #region AOB SCAN

        public async Task<IEnumerable<long>> AoBScan(string pattern)
        {
            return await Task.Run(() =>
            {
                PatternData pd = ParsePattern(pattern);
                List<long> results = new List<long>();
                IntPtr address = IntPtr.Zero;

                while (VirtualQueryEx(
                    ProcessHandle,
                    address,
                    out var mbi,
                    (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != 0)
                {
                    if (mbi.State == 0x1000 &&
                        mbi.Type == 0x20000 &&
                        mbi.Protect == 0x04)
                    {
                        int size = (int)mbi.RegionSize;
                        byte[] buffer = new byte[size];

                        if (ReadProcessMemory(
                            ProcessHandle,
                            mbi.BaseAddress,
                            buffer,
                            (IntPtr)size,
                            out _))
                        {
                            for (int i = 0; i <= size - pd.Pattern.Length; i++)
                            {
                                bool found = true;
                                for (int j = 0; j < pd.Pattern.Length; j++)
                                {
                                    if (pd.Mask[j] == 0xFF &&
                                        buffer[i + j] != pd.Pattern[j])
                                    {
                                        found = false;
                                        break;
                                    }
                                }
                                if (found)
                                    results.Add((long)mbi.BaseAddress + i);
                            }
                        }
                    }

                    address = (IntPtr)((long)mbi.BaseAddress + (long)mbi.RegionSize);
                }

                return results;
            });
        }

        PatternData ParsePattern(string pattern)
        {
            var parts = pattern.Split(' ');
            return new PatternData
            {
                Pattern = parts.Select(x => x == "??"
                    ? (byte)0
                    : byte.Parse(x, NumberStyles.HexNumber)).ToArray(),

                Mask = parts.Select(x => x == "??"
                    ? (byte)0
                    : (byte)0xFF).ToArray()
            };
        }

        #endregion
    }

    [Flags]
    public enum ProcessAccessFlags
    {
        AllAccess = 0x001F0FFF
    }
}
