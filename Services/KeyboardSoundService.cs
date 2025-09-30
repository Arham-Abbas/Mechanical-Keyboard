using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

        private readonly ConcurrentDictionary<int, bool> _keyStates = new();

        private readonly WaveOutEvent? _playbackDevice;
        private readonly MixingSampleProvider? _mixer;
        private readonly VolumeSampleProvider? _volumeProvider;

        private readonly Dictionary<int, SoundVariantPool> _soundMap = [];
        private readonly Random _random = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public double Volume
        {
            get => _volumeProvider?.Volume ?? 1.0f;
            set
            {
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

        public KeyboardSoundService(string soundPackDirectory)
        {
            LoadSoundPack(soundPackDirectory);
            _proc = HookCallback;

            if (_soundMap.Count > 0)
            {
                var waveFormat = _soundMap.First().Value.WaveFormat;
                _playbackDevice = new WaveOutEvent { DesiredLatency = 75, NumberOfBuffers = 2 };
                _mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
                _volumeProvider = new VolumeSampleProvider(_mixer);

                var settings = new SettingsService();
                Volume = settings.CurrentSettings.Volume;

                _playbackDevice.Init(_volumeProvider);
                _playbackDevice.Play();
            }
        }

        private void LoadSoundPack(string directory)
        {
            _soundMap.Clear();

            var rawSounds = new Dictionary<string, CachedSound>();
            var soundNames = new[] { "key-press", "space-press", "enter-press", "backspace-press", "modifier-press" };
            foreach (var name in soundNames)
            {
                var filePath = Path.Combine(directory, $"{name}.wav");
                if (File.Exists(filePath))
                {
                    try
                    {
                        rawSounds[name] = new CachedSound(filePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to load sound file '{filePath}': {ex.Message}");
                    }
                }
            }

            if (!rawSounds.TryGetValue("key-press", out var baseSound))
            {
                return;
            }

            var standardKeyPool = new SoundVariantPool(baseSound, 5);

            var keyMappings = new Dictionary<string, int[]>
            {
                { "space-press", [0x20] },
                { "enter-press", [0x0D] },
                { "backspace-press", [0x08] },
                { "modifier-press", [0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5] }
            };

            foreach (var mapping in keyMappings)
            {
                var soundName = mapping.Key;
                var keyCodes = mapping.Value;
                
                SoundVariantPool poolToUse = rawSounds.TryGetValue(soundName, out var specialSound) 
                    ? new SoundVariantPool(specialSound, 1) 
                    : standardKeyPool;

                foreach (var vkCode in keyCodes)
                {
                    _soundMap[vkCode] = poolToUse;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                if (!_soundMap.ContainsKey(i))
                {
                    _soundMap[i] = standardKeyPool;
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    var kbdStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var vkCode = kbdStruct.vkCode;
                    var msg = wParam.ToInt32();

                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    {
                        if (_keyStates.TryAdd(vkCode, true))
                        {
                            PlaySoundForKey(vkCode);
                        }
                    }
                    else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                    {
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

        private void PlaySoundForKey(int vkCode)
        {
            if (_mixer == null) return;

            if (_soundMap.TryGetValue(vkCode, out var pool))
            {
                var provider = pool.GetProvider(_random);
                if (provider != null)
                {
                    _mixer.AddMixerInput(provider);
                }
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

        public void Dispose()
        {
            Stop();
            _playbackDevice?.Dispose();
            GC.SuppressFinalize(this);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            App.DispatcherQueue?.TryEnqueue(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public async Task ReloadSoundPackAsync(string soundPackDirectory)
        {
            // Stop the current hook to prevent issues while loading
            Stop();

            // Load the new sound pack on a background thread
            await Task.Run(() => LoadSoundPack(soundPackDirectory));

            // Re-initialize the audio engine if sounds were loaded
            if (_soundMap.Count > 0)
            {
                if (_playbackDevice != null)
                {
                    // If the engine already exists, just re-initialize it
                    var waveFormat = _soundMap.First().Value.WaveFormat;
                    _mixer?.RemoveAllMixerInputs();
                    // Can't change the mixer's format, so a new engine is safer
                }
                // For simplicity and safety, just re-run the constructor's logic
            }

            // Restart the hook if the app is still enabled
            if (IsEnabled)
            {
                Start();
            }
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

    internal class SoundVariantPool
    {
        private readonly List<ResettableSampleProvider> _providers = [];
        public WaveFormat WaveFormat { get; }

        public SoundVariantPool(CachedSound baseSound, int variantCount)
        {
            WaveFormat = baseSound.WaveFormat;
            if (variantCount <= 1)
            {
                _providers.Add(new ResettableSampleProvider(baseSound));
            }
            else
            {
                for (int i = 0; i < variantCount; i++)
                {
                    float pitch = 1.0f + (i - variantCount / 2.0f) * 0.08f;
                    var pitchedSound = CreatePitchedVariant(baseSound, pitch);
                    _providers.Add(new ResettableSampleProvider(pitchedSound));
                }
            }
        }

        public ISampleProvider? GetProvider(Random random)
        {
            if (_providers.Count == 0) return null;
            
            var provider = _providers[random.Next(_providers.Count)];
            provider.Reset();
            return provider;
        }

        private static CachedSound CreatePitchedVariant(CachedSound source, float pitchFactor)
        {
            var sourceProvider = new ResettableSampleProvider(source);
            var resampler = new WdlResamplingSampleProvider(sourceProvider, (int)(source.WaveFormat.SampleRate * pitchFactor));
            var wholeFile = new List<float>((int)(source.AudioData.Length / pitchFactor));
            var readBuffer = new float[resampler.WaveFormat.SampleRate * resampler.WaveFormat.Channels];
            int samplesRead;
            while ((samplesRead = resampler.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }
            return new CachedSound([.. wholeFile], source.WaveFormat);
        }
    }

    internal class ResettableSampleProvider(CachedSound cachedSound) : ISampleProvider
    {
        private long _position;
        public WaveFormat WaveFormat => cachedSound.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var available = cachedSound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(available, count);
            if (samplesToCopy > 0)
            {
                Array.Copy(cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
                _position += samplesToCopy;
            }
            return (int)samplesToCopy;
        }

        public void Reset() => _position = 0;
    }
}
