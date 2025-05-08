using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public static class SoundAlerts
{
    private static Dictionary<string, string> soundMappings = new();

    public static bool Enabled = false;
    public static Func<string, Task> DebugLog = null;
    public static Action<bool> OnPlaybackStateChanged = null;

    public static void SetSoundMappings(Dictionary<string, string> mappings)
    {
        soundMappings = mappings ?? new();
        _ = TryLog("🔄 Sound mappings updated.");
    }

    public static async Task TryHandleMessage(string message)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(message))
            return;

        string trimmed = message.Trim().ToLowerInvariant();

        if (soundMappings.TryGetValue(trimmed, out string soundPath) && File.Exists(soundPath))
        {
            AudioQueue.Enqueue(soundPath);
            await TryLog($"🔔 Triggered sound alert for '{message}': {soundPath}");
        }
        else
        {
            await TryLog($"⚠️ No matching sound for command: '{message}'");
        }
    }

    public static float GetVolume() => AudioQueue.GetVolume();

    public static void StopAll()
    {
        AudioQueue.StopAll();
        _ = TryLog("🛑 All sound alerts stopped.");
    }

    public static void RegisterDebugLogger(Func<string, Task> logger)
    {
        DebugLog = logger;
        AudioQueue.DebugLog = logger;
    }

    public static void RegisterPlaybackObserver(Action<bool> onStateChanged)
    {
        OnPlaybackStateChanged = onStateChanged;
        AudioQueue.OnPlaybackStateChanged = onStateChanged;
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
