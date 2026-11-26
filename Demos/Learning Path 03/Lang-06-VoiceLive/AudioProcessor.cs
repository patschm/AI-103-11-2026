using Azure.AI.VoiceLive;
using NAudio.Wave;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;

namespace Lang_06_VoiceLive
{
    internal class AudioProcessor : IDisposable
    {
        private VoiceLiveSession _session;
        // NAudio components
        private WaveIn? _waveIn;
        private WaveOut? _waveOut;
        private BufferedWaveProvider? _playbackBuffer;

        // Audio configuration - PCM16, 24kHz, mono as specified
        private const int SampleRate = 24000;
        private const int Channels = 1;
        private const int BitsPerSample = 16;

        // Audio capture and playback state
        private bool _isCapturing;
        private bool _isPlaying;

        // Audio streaming channels
        private readonly Channel<byte[]> _audioSendChannel;
        private readonly Channel<byte[]> _audioPlaybackChannel;
        private readonly ChannelWriter<byte[]> _audioSendWriter;
        private readonly ChannelReader<byte[]> _audioSendReader;
        private readonly ChannelWriter<byte[]> _audioPlaybackWriter;
        private readonly ChannelReader<byte[]> _audioPlaybackReader;

        // Background tasks
        private Task? _audioSendTask;
        private Task? _audioPlaybackTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _playbackCancellationTokenSource;

        public AudioProcessor(VoiceLiveSession session)
        {
            this._session = session;
            // Create unbounded channels for audio data
            _audioSendChannel = Channel.CreateUnbounded<byte[]>();
            _audioSendWriter = _audioSendChannel.Writer;
            _audioSendReader = _audioSendChannel.Reader;
            _audioPlaybackChannel = Channel.CreateUnbounded<byte[]>();
            _audioPlaybackWriter = _audioPlaybackChannel.Writer;
            _audioPlaybackReader = _audioPlaybackChannel.Reader;
            _cancellationTokenSource = new CancellationTokenSource();
            _playbackCancellationTokenSource = new CancellationTokenSource();
        }

        public async Task QueueAudioAsync(byte[] audioData)
        {
            if (_isPlaying && audioData.Length > 0)
            {
                await _audioPlaybackWriter.WriteAsync(audioData);
            }
        }
        public Task StartCaptureAsync()
        {
            if (_isCapturing)
                return Task.CompletedTask;
            if (WaveInEvent.DeviceCount == 0)
            {
                throw new InvalidOperationException(
                    "No microphone (audio input device) was found. Attach a microphone, or if you are on a " +
                    "remote desktop/VM, enable 'Record from this computer' under Remote audio settings and reconnect.");
            }
            _isCapturing = true;

            try
            {
                _waveIn = new WaveIn
                {
                    WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
                    BufferMilliseconds = 50 // 50ms buffer for low latency
                };
                _waveIn.DataAvailable += OnAudioDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;
                _waveIn.DeviceNumber = 0;
                _waveIn.StartRecording();

                // Start audio send task
                _audioSendTask = ProcessAudioSendAsync(_cancellationTokenSource.Token);
                return Task.CompletedTask;
            }
            catch
            {
                _isCapturing = false;
                throw;
            }
        }
        public async Task StopCaptureAsync()
        {
            if (!_isCapturing)
                return;

            _isCapturing = false;

            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnAudioDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
            }

            // Complete the send channel and wait for the send task
            _audioSendWriter.TryComplete();
            if (_audioSendTask != null)
            {
                await _audioSendTask;
                _audioSendTask = null;
            }
        }
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Debug.WriteLine(e.Exception, "Audio recording stopped due to error");
            }
        }
        private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_isCapturing && e.BytesRecorded > 0)
            {
                byte[] audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, 0, audioData, 0, e.BytesRecorded);

                // Queue audio data for sending (non-blocking)
                if (!_audioSendWriter.TryWrite(audioData))
                {
                    Debug.WriteLine("Failed to queue audio data for sending - channel may be full");
                }
            }
        }
        private async Task? ProcessAudioSendAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (byte[] audioData in _audioSendReader.ReadAllAsync())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    try
                    {
                        // Send audio data directly to the session using the convenience method
                        // This demonstrates the existing SendInputAudioAsync convenience method
                        // Other available methods: StartAudioTurnAsync, AppendAudioToTurnAsync, EndAudioTurnAsync
                        await _session.SendInputAudioAsync(audioData);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex, "Error sending audio data to VoiceLive");
                        // Continue processing other audio data
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Error in audio send processing");
            }
        }
        public Task StartPlaybackAsync()
        {
            if (_isPlaying)
                return Task.CompletedTask;

            _isPlaying = true;

            try
            {
                _waveOut = new WaveOut
                {
                    BufferMilliseconds = 50 // 2 buffers x 50ms = ~100ms latency
                };

                _playbackBuffer = new BufferedWaveProvider(new WaveFormat(SampleRate, BitsPerSample, Channels), TimeSpan.FromMinutes(5))
                {
                    DiscardOnBufferOverflow = true
                };

                _waveOut.Init(_playbackBuffer);
                _waveOut.Play();

                _playbackCancellationTokenSource = new CancellationTokenSource();

                // Start audio playback task
                _audioPlaybackTask = ProcessAudioPlaybackAsync();
                return Task.CompletedTask;

            }
            catch
            {
                _isPlaying = false;
                throw;
            }
        }
        private async Task? ProcessAudioPlaybackAsync()
        {
            try
            {
                CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_playbackCancellationTokenSource.Token, _cancellationTokenSource.Token);
                var cancellationToken = combinedTokenSource.Token;

                await foreach (byte[] audioData in _audioPlaybackReader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    try
                    {
                        if (_playbackBuffer != null && _isPlaying)
                        {
                            _playbackBuffer.AddSamples(audioData, 0, audioData.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex, "Error in audio playback");
                        // Continue processing other audio data
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Error in audio playback processing");
            }
        }
        public async Task StopPlaybackAsync()
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;

            // Clear the playback channel
            while (_audioPlaybackReader.TryRead(out _))
            { }

            if (_playbackBuffer != null)
            {
                _playbackBuffer.ClearBuffer();
            }

            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            _playbackBuffer = null;

            _playbackCancellationTokenSource.Cancel();

            if (_audioPlaybackTask != null)
            {
                await _audioPlaybackTask;
                _audioPlaybackTask = null;
            }
        }
        private async Task CleanupAsync()
        {
            await StopCaptureAsync().ConfigureAwait(false);
            await StopPlaybackAsync().ConfigureAwait(false);

            _cancellationTokenSource.Cancel();

            // Wait for background tasks to complete
            var tasks = new List<Task>();
            if (_audioSendTask != null)
                tasks.Add(_audioSendTask);
            if (_audioPlaybackTask != null)
                tasks.Add(_audioPlaybackTask);

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            Console.WriteLine("Audio processor cleaned up");
        }


        public void Dispose()
        {
            CleanupAsync().Wait();
        }
    }
}