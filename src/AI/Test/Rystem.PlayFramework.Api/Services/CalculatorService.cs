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

    public double Add(double a, double b)
    {
        var result = a + b;
        _logger.LogInformation("Add: {A} + {B} = {Result}", a, b, result);
        return result;
    }

    public double Subtract(double a, double b)
    {
        var result = a - b;
        _logger.LogInformation("Subtract: {A} - {B} = {Result}", a, b, result);
        return result;
    }

    public double Multiply(double a, double b)
    {
        var result = a * b;
        _logger.LogInformation("Multiply: {A} × {B} = {Result}", a, b, result);
        return result;
    }

    public double Divide(double a, double b)
    {
        if (b == 0)
        {
            _logger.LogWarning("Attempted division by zero: {A} / 0", a);
            throw new DivideByZeroException("Cannot divide by zero");
        }

        var result = a / b;
        _logger.LogInformation("Divide: {A} ÷ {B} = {Result}", a, b, result);
        return result;
    }
}
