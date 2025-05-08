using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

public static class AudioQueue
{
    private static readonly ConcurrentQueue<string> queue = new();
    private static Task playbackTask;
    private static CancellationTokenSource cancelToken = new();
    private static float volume = 1.0f;
    private static WaveOutEvent currentOutput;

    public static Action<bool> OnPlaybackStateChanged;
    public static Func<string, Task> DebugLog;

    public static void SetVolume(float vol)
    {
        volume = Math.Clamp(vol, 0.01f, 1f);
    }

    public static float GetVolume() => volume;

    public static void Enqueue(string path)
    {
        if (!File.Exists(path))
        {
            _ = TryLog($"⚠️ File not found: {path}");
            return;
        }

        queue.Enqueue(path);
        _ = TryLog($"📥 Enqueued: {Path.GetFileName(path)}");
        StartPlayback();
    }

    public static void StopAll()
    {
        cancelToken.Cancel();
        while (queue.TryDequeue(out _)) { }

        if (currentOutput != null)
        {
            currentOutput.Stop();
            currentOutput.Dispose();
            currentOutput = null;
        }

        OnPlaybackStateChanged?.Invoke(false);
        _ = TryLog("🛑 Playback queue cleared and stopped.");
    }

    private static void StartPlayback()
    {
        if (playbackTask != null && !playbackTask.IsCompleted) return;

        cancelToken = new CancellationTokenSource();
        var token = cancelToken.Token;

        playbackTask = Task.Run(async () =>
        {
            OnPlaybackStateChanged?.Invoke(true);
            await TryLog("▶️ Starting audio playback loop.");

            while (!token.IsCancellationRequested && queue.TryDequeue(out var file))
            {
                try
                {
                    using var audio = new AudioFileReader(file) { Volume = volume };
                    currentOutput = new WaveOutEvent();
                    currentOutput.Init(audio);
                    currentOutput.Play();

                    await TryLog($"🔊 Playing: {Path.GetFileName(file)} at {volume * 100:F0}%");

                    while (currentOutput.PlaybackState == PlaybackState.Playing)
                        await Task.Delay(200, token);

                    currentOutput.Dispose();
                    currentOutput = null;

                    await Task.Delay(1000, token);
                }
                catch (Exception ex)
                {
                    await TryLog($"❌ Error playing sound: {ex.Message}");
                }
            }

            OnPlaybackStateChanged?.Invoke(false);
            await TryLog("⏹️ Audio playback loop finished.");
        }, token);
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
