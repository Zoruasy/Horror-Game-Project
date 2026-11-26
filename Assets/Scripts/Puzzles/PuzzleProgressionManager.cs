using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks first-time puzzle completions for the current play session.</summary>
public static class PuzzleProgressionManager
{
    static readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);

    public static event Action<string> PuzzleCompleted;

    public static int CompletedCount => completed.Count;

    public static bool IsCompleted(string puzzleId) => completed.Contains(puzzleId);

    public static bool AreCompleted(IEnumerable<string> puzzleIds)
    {
        foreach (string puzzleId in puzzleIds)
            if (!IsCompleted(puzzleId)) return false;

        return true;
    }

    public static bool MarkCompleted(string puzzleId)
    {
        if (string.IsNullOrWhiteSpace(puzzleId) || !completed.Add(puzzleId)) return false;

        PuzzleCompleted?.Invoke(puzzleId);
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        completed.Clear();
        PuzzleCompleted = null;
    }
}
