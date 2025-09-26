using System;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mechanical_Keyboard.Services
{
    public partial class KeyboardSoundService : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc? _proc;
        private readonly string? _soundFilePath;
        private bool _isRunning;

        // Delegate for the hook procedure
        private unsafe delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public KeyboardSoundService(string? soundFilePath)
        {
            _soundFilePath = soundFilePath;
            _proc = HookCallback;
        }

        public void Start()
        {
            if (_isRunning) return;
            _hookID = SetHook(_proc!);
            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning) return;
            UnhookWindowsHookEx(_hookID);
            _isRunning = false;
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == WM_KEYDOWN)
            {
                _ = PlaySoundAsync();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private async Task PlaySoundAsync()
        {
            if (string.IsNullOrEmpty(_soundFilePath)) return;
            await Task.Run(() =>
            {
                try
                {
                    using var player = new SoundPlayer(_soundFilePath);
                    player.PlaySync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error playing sound: {ex.Message}");
                }

                return Task.CompletedTask;
            });
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        // LibraryImport declarations for best performance (requires .NET 8+ and unsafe code enabled)
        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnhookWindowsHookEx(IntPtr hhk);

        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
    }
}
