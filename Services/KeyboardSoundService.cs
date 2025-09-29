using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mechanical_Keyboard.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mechanical_Keyboard.Services
{
    public partial class KeyboardSoundService : IDisposable, INotifyPropertyChanged
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc? _proc;
        private bool _isRunning;
        private bool _isEnabled;
        private readonly CachedSound? _cachedSound;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Ensure the event is raised on the UI thread for UI updates
            App.DispatcherQueue?.TryEnqueue(() =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
            );
        }

        private readonly ConcurrentDictionary<int, bool> _keyStates = new();

        private readonly WaveOutEvent? _playbackDevice;
        private readonly MixingSampleProvider? _mixer;
        // Add a VolumeSampleProvider to control the master volume
        private readonly VolumeSampleProvider? _volumeProvider;

        public double Volume
        {
            get => _volumeProvider?.Volume ?? 1.0f;
            set
            {
                // Volume is a float from 0.0 to 1.0 (or higher for amplification)
                _volumeProvider?.Volume = (float)Math.Clamp(value, 0.0, 2.0);
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                if (_isEnabled) Start(); else Stop();
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        private unsafe delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public KeyboardSoundService(string? soundFilePath)
        {
            if (!string.IsNullOrEmpty(soundFilePath))
            {
                try
                {
                    _cachedSound = new CachedSound(soundFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Failed to load sound file: {ex.Message}");
                }
            }
            _proc = HookCallback;

            if (_cachedSound != null)
            {
                _playbackDevice = new WaveOutEvent { DesiredLatency = 80, NumberOfBuffers = 2 };
                _mixer = new MixingSampleProvider(_cachedSound.WaveFormat) { ReadFully = true };
                
                // Create the volume provider and chain it to the mixer
                _volumeProvider = new VolumeSampleProvider(_mixer);

                // Set initial volume from settings
                var settings = new SettingsService();
                Volume = settings.CurrentSettings.Volume;
                _volumeProvider.Volume = (float)(settings.CurrentSettings.Volume / 100.0);
                // Initialize the playback device with the volume provider
                _playbackDevice.Init(_volumeProvider);
                _playbackDevice.Play();
            }
        }

        private void Start()
        {
            if (_isRunning || _proc == null) return;
            _hookID = SetHook(_proc);
            _isRunning = true;
        }

        private void Stop()
        {
            if (!_isRunning) return;
            UnhookWindowsHookEx(_hookID);
            _keyStates.Clear();
            _isRunning = false;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    var kbdStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var vkCode = kbdStruct.vkCode;
                    var wParamValue = wParam.ToInt32();

                    if (wParamValue == WM_KEYDOWN || wParamValue == WM_SYSKEYDOWN)
                    {
                        // If TryAdd succeeds, the key was not previously down. This is the initial press.
                        if (_keyStates.TryAdd(vkCode, true))
                        {
                            PlaySound();
                        }
                    }
                    else if (wParamValue == WM_KEYUP || wParamValue == WM_SYSKEYUP)
                    {
                        // When the key is released, remove it from the dictionary.
                        _keyStates.TryRemove(vkCode, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FATAL] Unhandled exception in keyboard hook: {ex}");
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void PlaySound()
        {
            if (_cachedSound == null || _mixer == null) return;

            var soundSampleProvider = new CachedSoundSampleProvider(_cachedSound);
            _mixer.AddMixerInput(soundSampleProvider);
        }

        public void Dispose()
        {
            Stop();
            _playbackDevice?.Dispose();
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        private static partial IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [LibraryImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnhookWindowsHookEx(IntPtr hhk);

        [LibraryImport("user32.dll", EntryPoint = "CallNextHookEx", SetLastError = true)]
        private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
    }

    public class CachedSoundSampleProvider(CachedSound cachedSound) : ISampleProvider
    {
        private long _position;
        public WaveFormat WaveFormat => cachedSound.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = cachedSound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);
            if (samplesToCopy > 0)
            {
                Array.Copy(cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
                _position += samplesToCopy;
            }
            return (int)samplesToCopy;
        }
    }
}
