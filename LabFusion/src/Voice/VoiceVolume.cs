using System.Collections.Concurrent;
using MelonLoader;
using LabFusion.Preferences.Client;

namespace LabFusion.Voice;

public static class VoiceVolume
{
    public const float DefaultSampleMultiplier = 10f;

    public const float MinimumVoiceVolume = 0.3f;

    public const float SilencingVolume = 0.1f;

    public const float TalkTimeoutTime = 1f;

    // Per-player multiplier limits (per-player cannot exceed 1.0 as requested)
    private const float PerPlayerMin = 0.0f;
    private const float PerPlayerMax = 1.0f;

    // Thread-safe store for per-player volume (keyed by peer ID)
    private static readonly ConcurrentDictionary<int, float> _perPlayerVolumes = new();

    // Return the global multiplier (keeps existing behavior)
    public static float GetGlobalMultiplier()
    {
        return ClientSettings.VoiceChat.GlobalVolume.Value;
    }

    // Return per-player multiplier (defaults to 1.0)
    public static float GetPlayerMultiplier(int peerId)
    {
        if (_perPlayerVolumes.TryGetValue(peerId, out var v))
            return v;

        return 1.0f;
    }

    // Set per-player multiplier. Clamped to [PerPlayerMin, PerPlayerMax].
    public static void SetPlayerMultiplier(int peerId, float multiplier)
    {
        float clamped = ClampPerPlayer(multiplier);
        _perPlayerVolumes.AddOrUpdate(peerId, clamped, (k, old) => clamped);
        MelonLogger.Msg($"Voice: set player {peerId} multiplier = {clamped}");
    }

    // Remove stored per-player override (reset to default 1.0)
    public static void RemovePlayerMultiplier(int peerId)
    {
        _perPlayerVolumes.TryRemove(peerId, out _);
        MelonLogger.Msg($"Voice: removed player {peerId} multiplier (reset to 1.0)");
    }

    // Combined multiplier = global * per-player
    public static float GetCombinedMultiplier(int peerId)
    {
        return GetGlobalMultiplier() * GetPlayerMultiplier(peerId);
    }

    private static float ClampPerPlayer(float v)
    {
        if (v < PerPlayerMin) return PerPlayerMin;
        if (v > PerPlayerMax) return PerPlayerMax;
        return v;
    }

    // Helper to set global multiplier programmatically (persisted via prefs)
    public static void SetGlobalMultiplier(float value, bool persist = true)
    {
        ClientSettings.VoiceChat.GlobalVolume.Value = value;
        if (persist)
            MelonPreferences.Save();
        MelonLogger.Msg($"Voice: set global multiplier = {value} (persist={persist})");
    }
}
