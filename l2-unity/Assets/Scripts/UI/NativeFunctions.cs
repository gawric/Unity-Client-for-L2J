using System.Runtime.InteropServices;
#if !UNITY_EDITOR_WIN && (!UNITY_STANDALONE_WIN || UNITY_EDITOR)
using UnityEngine;
using UnityEngine.InputSystem;
#endif

public class NativeFunctions {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int x, int y);
#else
    // Input System coordinates are relative to the game window on macOS/Linux.
    public static bool SetCursorPos(int x, int y) {
        Mouse mouse = Mouse.current;
        if (mouse == null) return false;

        mouse.WarpCursorPosition(new Vector2(x, y));
        return true;
    }
#endif

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeCoords {
        public int X;
        public int Y;
    }

#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out NativeCoords lpPoint);
#else
    public static bool GetCursorPos(out NativeCoords lpPoint) {
        Mouse mouse = Mouse.current;
        lpPoint = default;
        if (mouse == null) return false;

        Vector2 position = mouse.position.ReadValue();
        lpPoint.X = Mathf.RoundToInt(position.x);
        lpPoint.Y = Mathf.RoundToInt(position.y);
        return true;
    }
#endif
}
