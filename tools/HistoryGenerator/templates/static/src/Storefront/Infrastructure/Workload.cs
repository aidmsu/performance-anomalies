using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Storefront.Infrastructure;

/// <summary>
/// Spends a precise amount of wall-clock time on behalf of simulated work.
/// </summary>
/// <remarks>
/// Storefront has no real database, disk or network. Components describe the external work they do through the
/// cost primitives in this folder (<see cref="SimulatedDatabase"/>, <see cref="SimulatedStorage"/>,
/// <see cref="SimulatedNetwork"/>, <see cref="Compute"/>), and those primitives end up here.
/// <para>
/// On Linux and Windows a wait sleeps for most of the duration and spins for the rest, because
/// <see cref="Thread.Sleep(TimeSpan)"/> alone is too coarse (about 15.6 ms on Windows). On macOS it only spins:
/// sleeping threads on macOS CI virtual machines wake up 15-50 ms late. Every call gets a small multiplicative
/// jitter, like real I/O.
/// </para>
/// <para>
/// Decisions that depend on chance (cache hits, retries, contention) use <see cref="Random(string)"/>. On CI the
/// generator is seeded from the commit, the platform and the run attempt (GITHUB_SHA, GITHUB_RUN_ATTEMPT), so a
/// re-run of the same commit on the same platform takes the same decisions. Without those variables it is random.
/// </para>
/// </remarks>
public static class Workload
{
    private const double JitterSigma = 0.03;
    private const int NoisyMachineModulus = 40;
    private const double NoisyMachineFactor = 1.35;

    private static readonly bool CanSleep = !OperatingSystem.IsMacOS();

    private static readonly TimeSpan SleepMargin =
        OperatingSystem.IsWindows() ? TimeSpan.FromMilliseconds(20) : TimeSpan.FromMilliseconds(2);

    private static readonly string? CommitSha = ReadCommitSha();

    private static readonly string Attempt =
        Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT") is { Length: > 0 } attempt ? attempt.Trim() : "1";

    /// <summary>Platform the code runs on, for example <c>linux-x64</c> or <c>macos-arm64</c>.</summary>
    public static string Platform { get; } = DetectPlatform();

    /// <summary>
    /// Slowdown of the current machine. Usually 1; a few CI runs land on a noisy machine where all simulated work
    /// takes longer.
    /// </summary>
    public static double MachineFactor { get; } = DetectMachineFactor();

    /// <summary>Spends the given number of milliseconds.</summary>
    public static void Spend(double milliseconds)
    {
        if (milliseconds <= 0) return;
        var stopwatch = Stopwatch.StartNew();
        WaitUntil(stopwatch, Scale(milliseconds));
    }

    /// <summary>
    /// Waits until <paramref name="started"/> has been running for the given number of milliseconds. Real work done
    /// since the stopwatch started counts towards the cost.
    /// </summary>
    public static void PadTo(Stopwatch started, double milliseconds)
    {
        ArgumentNullException.ThrowIfNull(started);
        if (milliseconds <= 0) return;
        WaitUntil(started, Scale(milliseconds));
    }

    /// <summary>Random source for one decision point, identified by <paramref name="key"/>.</summary>
    public static Random Random(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return CommitSha is null ? System.Random.Shared : new Random(Seed($"{key}|{CommitSha}|{Platform}|{Attempt}"));
    }

    private static TimeSpan Scale(double milliseconds) =>
        TimeSpan.FromMilliseconds(milliseconds * MachineFactor * Math.Exp(JitterSigma * Gaussian()));

    private static void WaitUntil(Stopwatch stopwatch, TimeSpan duration)
    {
        if (CanSleep)
        {
            var sleep = duration - stopwatch.Elapsed - SleepMargin;
            if (sleep > TimeSpan.Zero) Thread.Sleep(sleep);
        }
        while (stopwatch.Elapsed < duration) Thread.SpinWait(20);
    }

    private static double Gaussian()
    {
        var u1 = 1.0 - System.Random.Shared.NextDouble();
        var u2 = System.Random.Shared.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private static string? ReadCommitSha()
    {
        var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
        return string.IsNullOrWhiteSpace(sha) ? null : sha.Trim().ToLowerInvariant();
    }

    private static string DetectPlatform()
    {
        var os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            var other => other.ToString().ToLowerInvariant(),
        };
        return $"{os}-{arch}";
    }

    private static double DetectMachineFactor() =>
        CommitSha is not null && Fnv1a($"bad-runner|{CommitSha}|{Platform}|{Attempt}") % NoisyMachineModulus == 0
            ? NoisyMachineFactor
            : 1.0;

    private static int Seed(string text) => (int)(Fnv1a(text) & 0x7FFFFFFF);

    private static ulong Fnv1a(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }
}
