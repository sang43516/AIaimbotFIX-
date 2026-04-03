using System;
using System.Numerics;
using TANISHregedit;

namespace AIMBOT_AI_SAFE
{
    internal static class ExternalCore
    {

// add nuget System.Numerics.Vectors


        public static Matrix4x4 CameraMatrix;
        public static int Width = 1920;
        public static int Height = 1080;
        public static bool HaveMatrix = false;

        // methods
        public const long VIEW_MATRIX_OFFSET = 0x1D2A3B0;

        public static void UpdateMatrix(TANISHaimbotAI mem, long libBase)
        {
            if (libBase == 0) return;

            byte[] buffer = mem.ReadBytes(libBase + VIEW_MATRIX_OFFSET, 64);
            if (buffer != null && buffer.Length == 64)
            {
                CameraMatrix = new Matrix4x4(
                    BitConverter.ToSingle(buffer, 0), BitConverter.ToSingle(buffer, 4), BitConverter.ToSingle(buffer, 8), BitConverter.ToSingle(buffer, 12),
                    BitConverter.ToSingle(buffer, 16), BitConverter.ToSingle(buffer, 20), BitConverter.ToSingle(buffer, 24), BitConverter.ToSingle(buffer, 28),
                    BitConverter.ToSingle(buffer, 32), BitConverter.ToSingle(buffer, 36), BitConverter.ToSingle(buffer, 40), BitConverter.ToSingle(buffer, 44),
                    BitConverter.ToSingle(buffer, 48), BitConverter.ToSingle(buffer, 52), BitConverter.ToSingle(buffer, 56), BitConverter.ToSingle(buffer, 60)
                );
                HaveMatrix = true;
            }
            else { HaveMatrix = false; }
        }
    }
}