using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring telemetry in PlayFramework.
/// </summary>
public static class PlayFrameworkBuilderTelemetryExtensions
{
    /// <summary>
    /// Configures telemetry and observability settings for PlayFramework.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="configure">Configuration action for telemetry settings.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// builder.WithTelemetry(telemetry =>
    /// {
    ///     telemetry.EnableTracing = true;
    ///     telemetry.EnableMetrics = true;
    ///     telemetry.TraceScenes = true;
    ///     telemetry.TraceTools = true;
    ///     telemetry.TraceLlmCalls = true;
    ///     telemetry.SamplingRate = 0.1; // 10% sampling for production
    ///     telemetry.CustomAttributes = new()
    ///     {
    ///         ["deployment.environment"] = "production",
    ///         ["service.version"] = "1.0.0"
    ///     };
    /// });
    /// </example>
    public static PlayFrameworkBuilder WithTelemetry(
        this PlayFrameworkBuilder builder,
        Action<TelemetrySettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Settings.Telemetry);

        return builder;
    }

    /// <summary>
    /// Enables tracing for all PlayFramework operations.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="samplingRate">Sampling rate (0.0 to 1.0). Default: 1.0 (100%).</param>
    /// <returns>The builder for chaining.</returns>
    public static PlayFrameworkBuilder WithTracing(
        this PlayFrameworkBuilder builder,
        double samplingRate = 1.0)
    {
        builder.Settings.Telemetry.EnableTracing = true;
        builder.Settings.Telemetry.SamplingRate = samplingRate;

        return builder;
    }

    /// <summary>
    /// Enables metrics collection for all PlayFramework operations.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static PlayFrameworkBuilder WithMetrics(
        this PlayFrameworkBuilder builder)
    {
        builder.Settings.Telemetry.EnableMetrics = true;

        return builder;
    }

    /// <summary>
    /// Disables telemetry completely (useful for testing or development).
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static PlayFrameworkBuilder WithoutTelemetry(
        this PlayFrameworkBuilder builder)
    {
        builder.Settings.Telemetry.EnableTracing = false;
        builder.Settings.Telemetry.EnableMetrics = false;

        return builder;
    } 
}
