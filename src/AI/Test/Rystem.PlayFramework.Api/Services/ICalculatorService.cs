namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Calculator service for basic arithmetic operations.
/// Methods are exposed as LLM tools via PlayFramework ServiceMethodTool pattern.
/// </summary>
public interface ICalculatorService
{
    /// <summary>
    /// Adds two numbers together.
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Sum of a and b</returns>
    double Add(double a, double b);

    /// <summary>
    /// Subtracts the second number from the first.
    /// </summary>
    /// <param name="a">Number to subtract from</param>
    /// <param name="b">Number to subtract</param>
    /// <returns>Difference (a - b)</returns>
    double Subtract(double a, double b);

    /// <summary>
    /// Multiplies two numbers together.
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Product of a and b</returns>
    double Multiply(double a, double b);

    /// <summary>
    /// Divides the first number by the second.
    /// </summary>
    /// <param name="a">Dividend (number to be divided)</param>
    /// <param name="b">Divisor (number to divide by)</param>
    /// <returns>Quotient (a / b)</returns>
    /// <exception cref="DivideByZeroException">Thrown when b is zero</exception>
    double Divide(double a, double b);
}
