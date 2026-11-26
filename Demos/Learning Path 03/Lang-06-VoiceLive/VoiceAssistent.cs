using Azure.AI.VoiceLive;
using System.Diagnostics;
using System.Threading;

namespace Lang_06_VoiceLive;

internal class VoiceAssistent
{
    private readonly VoiceLiveClient _client;
    private readonly string _model;
    private readonly string _voice;
    private readonly string _instructions;

    private VoiceLiveSession? _session;
    private AudioProcessor? _audioProcessor;

    public VoiceAssistent(VoiceLiveClient client, string model, string voice, string instructions)
    {
        _client = client;
        _model = model;
        _voice = voice;
        _instructions = instructions;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _session = await _client.StartSessionAsync(_model);
        _audioProcessor = new AudioProcessor(_session);
        await SetupSessionAsync();
        await _audioProcessor.StartPlaybackAsync();
        await _audioProcessor.StartCaptureAsync();
        Console.WriteLine();
        Console.WriteLine("=" + new string('=', 59));
        Console.WriteLine("* VOICE ASSISTANT READY");
        Console.WriteLine("Start speaking to begin conversation");
        Console.WriteLine("Press Ctrl+C to exit");
        Console.WriteLine("=" + new string('=', 59));
        Console.WriteLine();

        // Process events
        await ProcessEventsAsync(cancellationToken);
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
       await foreach (SessionUpdate svrEvent in _session!.GetUpdatesAsync())
        {
            await HandleSessionUpdateAsync(svrEvent, cancellationToken);
        }
    }

    private async Task HandleSessionUpdateAsync(SessionUpdate svrEvent, CancellationToken cancellationToken)
    {
        switch(svrEvent)
        {
            case SessionUpdateSessionCreated created:
            case SessionUpdateSessionUpdated updated:
                await _audioProcessor!.StartCaptureAsync();
                break;
            case SessionUpdateInputAudioBufferSpeechStarted start:
                Console.WriteLine("* Listening...");
                await _audioProcessor!.StopPlaybackAsync();
                await _session!.CancelResponseAsync(cancellationToken);
                await _session.ClearStreamingAudioAsync(cancellationToken);
                break;
            case SessionUpdateInputAudioBufferSpeechStopped stop:
                Console.WriteLine("* Processing...");
                await _audioProcessor!.StartPlaybackAsync();
                break;
            case SessionUpdateResponseCreated responseCreated:
                Debug.WriteLine("Assistent Response created");
                break;
            case SessionUpdateResponseAudioDelta audioDelta:
                if (audioDelta != null)
                {
                    var bytes = audioDelta.Delta.ToArray();
                    await _audioProcessor!.QueueAudioAsync(bytes);
                }
                break;
            case SessionUpdateResponseAudioDone audioDone:
                //await Task.Delay(500);
                Console.WriteLine("* Ready for next input...");
                break;
            case SessionUpdateResponseDone responseDone:
                break;

            case SessionUpdateError errorEvent:
                Console.WriteLine($"Error: {errorEvent.Error?.Message}");
                break;

            default:
                Debug.WriteLine($"Unhandled event type: {svrEvent.GetType().Name}");
                break;
        }
    }

    private async Task HandleSessionCreated(SessionUpdateSessionCreated created)
    {
        // Start audio capture once session is ready
        if (_audioProcessor != null)
        {
            await _audioProcessor.StartCaptureAsync();
        }
    }

    private async Task SetupSessionAsync()
    {
        var voice = new AzureStandardVoice(_voice);

        // When is it time for the other to speak
        var turnDetection = new ServerVadTurnDetection
        {
            Threshold = 0.5f,
            PrefixPadding = TimeSpan.FromMilliseconds(300),
            SilenceDuration = TimeSpan.FromMilliseconds(1000)
        };

        var options = new VoiceLiveSessionOptions
        {
            InputAudioEchoCancellation = new AudioEchoCancellation(),
            Model = _model,
            Instructions = _instructions,
            Voice = voice,
            TurnDetection = turnDetection,
            InputAudioFormat = InputAudioFormat.Pcm16,
            OutputAudioFormat = OutputAudioFormat.Pcm16,
        };
        options.Modalities.Clear();
        options.Modalities.Add(InteractionModality.Text);
        options.Modalities.Add(InteractionModality.Audio);

        await _session!.ConfigureSessionAsync(options);
    }
}
