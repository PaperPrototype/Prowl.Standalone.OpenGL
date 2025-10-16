// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Interface for input processors that transform input values.
/// </summary>
public interface IInputProcessor
{
    /// <summary>
    /// Process a double value.
    /// </summary>
    double Process(double value);

    /// <summary>
    /// Process a Vector2 value.
    /// </summary>
    Double2 Process(Double2 value);
}

/// <summary>
/// Normalizes vector input to have a magnitude of at most 1.
/// </summary>
public class NormalizeProcessor : IInputProcessor
{
    public double Process(double value) => Math.Clamp(value, -1f, 1f);

    public Double2 Process(Double2 value)
    {
        double magnitude = Math.Sqrt(value.X * value.X + value.Y * value.Y);
        if (magnitude > 1f)
            return value / magnitude;
        return value;
    }
}

/// <summary>
/// Inverts the input value.
/// </summary>
public class InvertProcessor : IInputProcessor
{
    public double Process(double value) => -value;
    public Double2 Process(Double2 value) => -value;
}

/// <summary>
/// Scales the input value by a multiplier.
/// </summary>
public class ScaleProcessor : IInputProcessor
{
    public double Scale { get; set; } = 1f;

    public ScaleProcessor(double scale)
    {
        Scale = scale;
    }

    public double Process(double value) => value * Scale;
    public Double2 Process(Double2 value) => value * Scale;
}

/// <summary>
/// Clamps the input value to a specified range.
/// </summary>
public class ClampProcessor : IInputProcessor
{
    public double Min { get; set; } = 0f;
    public double Max { get; set; } = 1f;

    public ClampProcessor(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public double Process(double value) => Math.Clamp(value, Min, Max);

    public Double2 Process(Double2 value)
    {
        return new Double2(
            Math.Clamp(value.X, Min, Max),
            Math.Clamp(value.Y, Min, Max)
        );
    }
}

/// <summary>
/// Applies a deadzone to the input value. Values below the threshold are set to zero.
/// </summary>
public class DeadzoneProcessor : IInputProcessor
{
    public double Threshold { get; set; } = 0.2f;

    public DeadzoneProcessor(double threshold = 0.2f)
    {
        Threshold = threshold;
    }

    public double Process(double value)
    {
        if (Math.Abs(value) < Threshold)
            return 0f;

        // Rescale the value to start from 0 after the deadzone
        double sign = Math.Sign(value);
        double adjustedValue = (Math.Abs(value) - Threshold) / (1f - Threshold);
        return sign * adjustedValue;
    }

    public Double2 Process(Double2 value)
    {
        double magnitude = Math.Sqrt(value.X * value.X + value.Y * value.Y);
        if (magnitude < Threshold)
            return Double2.Zero;

        // Radial deadzone - preserve direction
        double adjustedMagnitude = (magnitude - Threshold) / (1f - Threshold);
        return value * (adjustedMagnitude / magnitude);
    }
}

/// <summary>
/// Applies an exponential curve to the input for more precise control at low values.
/// </summary>
public class ExponentialProcessor : IInputProcessor
{
    public double Exponent { get; set; } = 2f;

    public ExponentialProcessor(double exponent = 2f)
    {
        Exponent = exponent;
    }

    public double Process(double value)
    {
        double sign = Math.Sign(value);
        return sign * Math.Pow(Math.Abs(value), Exponent);
    }

    public Double2 Process(Double2 value)
    {
        return new Double2(Process(value.X), Process(value.Y));
    }
}
