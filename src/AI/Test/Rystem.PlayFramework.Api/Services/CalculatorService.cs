using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Implementation of calculator service with basic arithmetic operations.
/// Thread-safe stateless implementation suitable for Singleton registration.
/// </summary>
public sealed class CalculatorService : ICalculatorService
{
    private readonly ILogger<CalculatorService> _logger;

    public CalculatorService(ILogger<CalculatorService> logger)
    {
        _logger = logger;
    }

    public double Add(
        [Description("The first number (augend) to add")] double augend,
        [Description("The second number (addend) to add")] double addend)
    {
        var result = augend + addend;
        _logger.LogInformation("Add: {Augend} + {Addend} = {Result}", augend, addend, result);
        return result;
    }

    public double Subtract(
        [Description("The number to subtract from (minuend)")] double minuend,
        [Description("The number to subtract (subtrahend)")] double subtrahend)
    {
        var result = minuend - subtrahend;
        _logger.LogInformation("Subtract: {Minuend} - {Subtrahend} = {Result}", minuend, subtrahend, result);
        return result;
    }

    public double Multiply(
        [Description("The first number to multiply (multiplicand)")] double multiplicand,
        [Description("The second number to multiply by (multiplier)")] double multiplier)
    {
        var result = multiplicand * multiplier;
        _logger.LogInformation("Multiply: {Multiplicand} × {Multiplier} = {Result}", multiplicand, multiplier, result);
        return result;
    }

    public double Divide(
        [Description("The number to be divided (dividend)")] double dividend,
        [Description("The number to divide by (divisor)")] double divisor)
    {
        if (divisor == 0)
        {
            _logger.LogWarning("Attempted division by zero: {Dividend} / 0", dividend);
            throw new DivideByZeroException("Cannot divide by zero");
        }

        var result = dividend / divisor;
        _logger.LogInformation("Divide: {Dividend} ÷ {Divisor} = {Result}", dividend, divisor, result);
        return result;
    }
}
