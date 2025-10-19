// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Prowl.Runtime;

public class TimeData
{
    public double UnscaledDeltaTime;
    public double UnscaledTotalTime;
    public double DeltaTime;
    public double Time;
    public double SmoothUnscaledDeltaTime;
    public double SmoothDeltaTime;

    private Stopwatch _stopwatch;

    public TimeData() { }

    public long FrameCount;

    public double TimeScale = 1f;
    public float TimeScaleF => (float)TimeScale;
    public double TimeSmoothFactor = .25f;

    public void Update()
    {
        _stopwatch ??= Stopwatch.StartNew();

        double dt = _stopwatch.Elapsed.TotalMilliseconds / 1000.0;

        FrameCount++;

        UnscaledDeltaTime = dt;
        UnscaledTotalTime += UnscaledDeltaTime;

        DeltaTime = dt * TimeScale;
        Time += DeltaTime;

        SmoothUnscaledDeltaTime += (dt - SmoothUnscaledDeltaTime) * TimeSmoothFactor;
        SmoothDeltaTime = SmoothUnscaledDeltaTime * TimeScale;

        _stopwatch.Restart();
    }
}

public static class Time
{

    public static Stack<TimeData> TimeStack { get; } = new();

    public static TimeData CurrentTime => TimeStack.Peek();

    public static double UnscaledDeltaTime => CurrentTime.UnscaledDeltaTime;
    public static double UnscaledTotalTime => CurrentTime.UnscaledTotalTime;

    public static double DeltaTime => CurrentTime.DeltaTime;
    public static float DeltaTimeF => (float)DeltaTime;
    public static double FixedDeltaTime => 1.0 / 60.0; // 60 FPS fixed timestep
    public static double TimeSinceStartup => CurrentTime.Time;

    public static double SmoothUnscaledDeltaTime => CurrentTime.SmoothUnscaledDeltaTime;
    public static double SmoothDeltaTime => CurrentTime.SmoothDeltaTime;

    public static long FrameCount => CurrentTime.FrameCount;

    public static double TimeScale
    {
        get => CurrentTime.TimeScale;
        set => CurrentTime.TimeScale = value;
    }

    public static float TimeScaleF
    {
        get => (float)TimeScale;
        set => TimeScale = value;
    }

    public static double TimeSmoothFactor
    {
        get => CurrentTime.TimeSmoothFactor;
        set => CurrentTime.TimeSmoothFactor = value;
    }
}
