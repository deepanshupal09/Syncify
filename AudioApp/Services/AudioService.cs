using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioApp.Services
{
    public class CircularBufferWaveProvider : IWaveProvider
    {
        private readonly byte[] _buffer;
        private int _writePosition;
        private int _readPosition;
        private int _byteCount;
        private readonly object _lockObj = new();
        private readonly WaveFormat _waveFormat;

        // Add buffer state tracking
        private int _bufferSize;
        public int TotalBytesWritten { get; private set; }
        public int TotalBytesRead { get; private set; }

        public CircularBufferWaveProvider(WaveFormat waveFormat, int bufferSize = 10 * 48000 * 4) // ~10s buffer
        {
            _waveFormat = waveFormat;
            _buffer = new byte[bufferSize];
            _bufferSize = bufferSize;

        }

        public WaveFormat WaveFormat => _waveFormat;

        public void AddSamples(byte[] data, int offset, int count)
        {
            lock (_lockObj)
            {
                // Always overwrite if buffer full (never throw)
                var bytesToWrite = Math.Min(count, _bufferSize);

                // Write in two parts if wrapping needed
                var firstPart = Math.Min(bytesToWrite, _bufferSize - _writePosition);
                Buffer.BlockCopy(data, offset, _buffer, _writePosition, firstPart);

                if (bytesToWrite > firstPart)
                {
                    Buffer.BlockCopy(data, offset + firstPart, _buffer, 0, bytesToWrite - firstPart);
                }

                _writePosition = (_writePosition + bytesToWrite) % _bufferSize;
                TotalBytesWritten += bytesToWrite;

                // Advance read position if buffer overflows
                var overflow = Math.Max(0, (TotalBytesWritten - TotalBytesRead) - _bufferSize);
                if (overflow > 0)
                {
                    _readPosition = (_readPosition + overflow) % _bufferSize;
                    TotalBytesRead += overflow;
                }
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            lock (_lockObj)
            {
                var bytesAvailable = TotalBytesWritten - TotalBytesRead;
                if (bytesAvailable == 0)
                {
                    // Return silence instead of 0
                    Array.Clear(buffer, offset, count);
                    return count;
                }

                var bytesToRead = Math.Min(count, bytesAvailable);
                var firstPart = Math.Min(bytesToRead, _bufferSize - _readPosition);

                Buffer.BlockCopy(_buffer, _readPosition, buffer, offset, firstPart);

                if (bytesToRead > firstPart)
                {
                    Buffer.BlockCopy(_buffer, 0, buffer, offset + firstPart, bytesToRead - firstPart);
                }

                _readPosition = (_readPosition + bytesToRead) % _bufferSize;
                TotalBytesRead += bytesToRead;

                return bytesToRead;
            }
        }
    }

    public class AudioService : IDisposable
    {
        private Thread? _audioThread;
        private WasapiLoopbackCapture? _capture;
        private Timer? _bufferMonitor;
        private readonly ConcurrentDictionary<string, (CircularBufferWaveProvider Provider, WasapiOut Player)> _outputs = new();
        private readonly List<string> _selectedIds = new();
        public IReadOnlyList<string> SelectedDevices => _selectedIds.AsReadOnly();
        private readonly object _syncLock = new();

        // Add these diagnostic fields
        private int _totalBytesCaptured;
        private int _lastBytesLogged;
        private DateTime _lastRateLog = DateTime.MinValue;

        // Add to AudioService class
        private DateTime _lastBufferLog;
        private int _lastBytesWritten;

        public bool IsRunning => _audioThread?.IsAlive ?? false;
        public bool IsStopped => !IsRunning;

        public void StartCapture()
        {
            lock (_syncLock)
            {
                if (IsRunning) return;

                _audioThread = new Thread(AudioCaptureMain)
                {
                    IsBackground = true,
                    Name = "AudioCaptureThread",
                    ApartmentState = ApartmentState.MTA
                };
                _audioThread.Start();

                // Start buffer health monitoring
                _bufferMonitor = new Timer(_ => CheckBufferHealth(), null, 2000, 2000);
            }
        }

        private void AudioCaptureMain()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                _capture = new WasapiLoopbackCapture(defaultDevice);
                _capture.DataAvailable += OnLoopbackData;
                _capture.StartRecording();

                try
                {
                    while (IsRunning)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (ThreadInterruptedException) { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio thread error: {ex}");
            }
            finally
            {
                _capture?.StopRecording();
                _capture?.Dispose();
                _capture = null;
            }
        }

        private void CheckBufferHealth()
        {
            foreach (var kv in _outputs.ToArray())
            {
                var (provider, player) = kv.Value;
                try
                {
                    if (player.PlaybackState == PlaybackState.Stopped)
                    {
                        Debug.WriteLine($"Restarting stuck player: {kv.Key}");
                        RemoveOutputDevice(kv.Key);
                        AddOutputDevice(kv.Key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Buffer health check failed: {ex}");
                }
            }
        }
        private void OnLoopbackData(object? sender, WaveInEventArgs e)
        {
            try
            {
                Interlocked.Add(ref _totalBytesCaptured, e.BytesRecorded);

                // Monitor data flow rate
                if ((DateTime.Now - _lastBufferLog).TotalSeconds > 5)
                {
                    Debug.WriteLine($"[Audio] Data rate: {(_totalBytesCaptured - _lastBytesWritten) / 1024} KB/s");
                    _lastBytesWritten = _totalBytesCaptured;
                    _lastBufferLog = DateTime.Now;
                }

                foreach (var kv in _outputs.ToArray())
                {
                    var provider = kv.Value.Provider;
                    provider.AddSamples(e.Buffer, 0, e.BytesRecorded);

                    // Log buffer state
                    Debug.WriteLine($"[Buffer] {kv.Key} - " +
                        $"Available: {provider.TotalBytesWritten - provider.TotalBytesRead} bytes");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical audio error: {ex}");
            }
        }

        public void AddOutputDevice(string deviceId)
        {
            lock (_syncLock)
            {
                var defaultId = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;

                if (deviceId == defaultId) return;

                if (!IsRunning || _outputs.ContainsKey(deviceId)) return;

                try
                {
                    using var enumerator = new MMDeviceEnumerator();
                    var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                        .FirstOrDefault(d => d.ID == deviceId);

                    if (device == null) return;

                    var format = _capture?.WaveFormat ?? new WaveFormat(44100, 16, 2);
                    var provider = new CircularBufferWaveProvider(format);

                    var player = new WasapiOut(device, AudioClientShareMode.Shared, true, 100);
                    player.Init(provider);
                    player.Play();

                    _outputs[deviceId] = (provider, player);
                    if (!_selectedIds.Contains(deviceId))
                        _selectedIds.Add(deviceId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AddOutput failed: {ex}");
                }
            }
        }

        public void RemoveOutputDevice(string deviceId)
        {
            //if (_selectedIds.Count == 1) return;

            lock (_syncLock)
            {
                if (_outputs.TryRemove(deviceId, out var tuple))
                {
                    tuple.Player.Stop();
                    tuple.Player.Dispose();
                    _selectedIds.Remove(deviceId);
                }
            }
        }

        public void RestartOnDefaultChange()
        {
            lock (_syncLock)
            {
                try
                {
                    // Get new default device ID BEFORE stopping capture
                    var newDefaultId = new MMDeviceEnumerator()
                        .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;

                    var previous = _selectedIds
                        .Where(id => id != newDefaultId) // Exclude new default device
                        .ToArray();

                    StopCapture();
                    StartCapture();

                    foreach (var id in previous)
                    {
                        if (DeviceExists(id) && id != newDefaultId)
                        {
                            AddOutputDevice(id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Restart failed: {ex}");
                }
            }
        }


        // Modify StopCapture to ensure full cleanup
        public void StopCapture()
        {
            lock (_syncLock)
            {
                try
                {
                    Debug.WriteLine("Stopping capture...");

                    // 1. Stop all outputs
                    foreach (var kv in _outputs.ToArray())
                    {
                        kv.Value.Player.Stop();
                        kv.Value.Player.Dispose();
                    }
                    _outputs.Clear();

                    // 2. Stop capture
                    _capture?.StopRecording();
                    _capture?.Dispose();
                    _capture = null;

                    // 3. Stop thread
                    if (_audioThread?.IsAlive == true)
                    {
                        _audioThread.Interrupt();
                        if (!_audioThread.Join(500))
                        {
                            _audioThread.Abort();
                        }
                    }
                    _audioThread = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"StopCapture error: {ex}");
                }
            }
        }

        public bool DeviceExists(string deviceId)
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .Any(d => d.ID == deviceId);
        }

        public void SetDeviceVolume(string deviceId, float level)
        {
            lock (_syncLock)
            {
                if (_outputs.TryGetValue(deviceId, out var tuple))
                {
                    // Clamp volume between 0.0-1.0 and set it
                    tuple.Player.Volume = Math.Clamp(level, 0f, 1f);
                }
            }
        }

        public void Dispose()
        {
            StopCapture();
            _bufferMonitor?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}