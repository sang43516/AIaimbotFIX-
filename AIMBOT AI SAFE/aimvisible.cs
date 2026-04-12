using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using TANISHregedit;

internal static class AimbotVisibleScan
{
   private static TANISHaimbotAI mem = new TANISHaimbotAI();       // mem connected private static TANISHaimbotAI mem = new TANISHaimbotAI();

    private static Thread aimThread;
    private static bool isRunning = false;
    // ================= CONFIG =================
    private const string PROCESS = "HD-Player";
    private const string AOB_PATTERN = "00 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A5 43";
    private const long HEAD_OFFSET = 0x95;
    private const long CHEST_OFFSET = 0x91;

    private static List<long> baseAddresses = new List<long>();
    private static Dictionary<long, int> patchedMemory = new Dictionary<long, int>();

    private static DateTime lastClickStart = DateTime.MinValue;
    private static bool isMouseHeld = false;

    private static DateTime lastRW = DateTime.MinValue;
    private const int RW_DELAY_MS = 142;

    // 🔒 1.5 sec hold lock
    private static bool holdLocked = false;

    internal static bool IsAlive()
    {
        return isRunning && aimThread != null && aimThread.IsAlive;
    }

    internal static async void Start()
    {
        if (isRunning) return;

        if (!mem.SetProcess(PROCESS))
        {
            MessageBox.Show("Emulator Not Found");
            return;
        }

        baseAddresses.Clear();
        patchedMemory.Clear();

        var scan = await mem.AoBScan(AOB_PATTERN);
        if (!scan.Any())
        {
            MessageBox.Show("Pattern Not Found");
            return;
        }

        baseAddresses.AddRange(scan);

        isRunning = true;
        aimThread = new Thread(MainLoop) { IsBackground = true };
        aimThread.Start();
    }

    private static void MainLoop()
    {
        while (isRunning)
        {
            try
            {
                bool mouseDown = (GetAsyncKeyState((int)Keys.LButton) & 0x8000) != 0;

                // 🔒 locked after 1.5 sec until release
                if (holdLocked)
                {
                    if (!mouseDown)
                    {
                        holdLocked = false;
                        isMouseHeld = false;
                        lastClickStart = DateTime.MinValue;
                    }

                    Thread.Sleep(5);
                    continue;
                }

                // RELEASE
                if (!mouseDown)
                {
                    if (isMouseHeld)
                        patchedMemory.Clear();

                    isMouseHeld = false;
                    lastClickStart = DateTime.MinValue;
                    Thread.Sleep(5);
                    continue;
                }

                // PRESS
                if (!isMouseHeld)
                {
                    isMouseHeld = true;
                    lastClickStart = DateTime.Now;
                    patchedMemory.Clear();
                }

                // ⏱ auto pause after 1.5 sec hold
                if ((DateTime.Now - lastClickStart).TotalMilliseconds >= 1500)
                {
                    patchedMemory.Clear();
                    holdLocked = true;
                    continue;
                }

                // RW limiter
                if ((DateTime.Now - lastRW).TotalMilliseconds < RW_DELAY_MS)
                {
                    Thread.Sleep(5);
                    continue;
                }

                lastRW = DateTime.Now;

                foreach (long baseAddr in baseAddresses)
                {
                    long headAddr = baseAddr + HEAD_OFFSET;
                    long chestAddr = baseAddr - CHEST_OFFSET;

                    byte[] headBytes = mem.ReadBytes(headAddr, 4);
                    byte[] chestBytes = mem.ReadBytes(chestAddr, 4);
                    if (headBytes == null || chestBytes == null) continue;

                    int head = BitConverter.ToInt32(headBytes, 0);
                    int chest = BitConverter.ToInt32(chestBytes, 0);
                    if (head == 0) continue;

                    if (!patchedMemory.ContainsKey(chestAddr))
                        patchedMemory[chestAddr] = chest;

                    mem.WriteBytes(chestAddr, BitConverter.GetBytes(head));
                }

                Thread.Sleep(2);
            }
            catch
            {
                Thread.Sleep(20);
            }
        }

        patchedMemory.Clear();
    }

    internal static void Stop()
    {
        isRunning = false;
        try { aimThread?.Join(300); } catch { }
        patchedMemory.Clear();
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
