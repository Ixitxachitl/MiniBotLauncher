using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

public static class SoundAlerts
{
    private static Dictionary<string, string> soundMappings = new();
    private static ConcurrentQueue<string> soundQueue = new();
    private static CancellationTokenSource tokenSource = new();
    private static Task playbackTask;

    public static bool Enabled = false;
    public static Func<string, Task> DebugLog = null;
    private static float volume = 1.0f;
    public static Action<bool> OnPlaybackStateChanged;

    public static void SetVolume(float vol)
    {
        volume = Math.Clamp(vol, 0f, 1f);
    }

    public static void StopAll()
    {
        tokenSource?.Cancel();
        while (soundQueue.TryDequeue(out _)) { }
        OnPlaybackStateChanged?.Invoke(false);
    }

    public static void SetSoundMappings(Dictionary<string, string> mappings)
    {
        soundMappings = mappings ?? new Dictionary<string, string>();
    }

    public static async Task TryHandleMessage(string message)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(message))
            return;

        string trimmed = message.Trim().ToLowerInvariant();

        if (soundMappings.TryGetValue(trimmed, out string soundPath) && File.Exists(soundPath))
        {
            soundQueue.Enqueue(soundPath);
            await TryLog($"Queued sound: {Path.GetFileName(soundPath)}");
            StartPlaybackLoop();
        }
    }

    private static void StartPlaybackLoop()
    {
        if (playbackTask != null && !playbackTask.IsCompleted)
            return;

        tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        playbackTask = Task.Run(async () =>
        {
            OnPlaybackStateChanged?.Invoke(true);

            while (!token.IsCancellationRequested && soundQueue.TryDequeue(out string file))
            {
                try
                {
                    await TryLog($"Playing: {Path.GetFileName(file)}");

                    using var audioFile = new AudioFileReader(file);
                    audioFile.Volume = volume;
                    using var outputDevice = new WaveOutEvent();
                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        await Task.Delay(200, token);
                    }

                    await Task.Delay(1000, token);
                }
                catch (Exception ex)
                {
                    await TryLog($"Error playing sound: {ex.Message}");
                }
            }

            OnPlaybackStateChanged?.Invoke(false);
        }, token);
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
