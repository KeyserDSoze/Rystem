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
    /// <param name="augend">First addend</param>
    /// <param name="addend">Second addend</param>
    /// <returns>Sum of augend and addend</returns>
    double Add(double augend, double addend);

    /// <summary>
    /// Subtracts the second number from the first.
    /// </summary>
    /// <param name="minuend">Number to subtract from</param>
    /// <param name="subtrahend">Number to subtract</param>
    /// <returns>Difference (minuend - subtrahend)</returns>
    double Subtract(double minuend, double subtrahend);

    /// <summary>
    /// Multiplies two numbers together.
    /// </summary>
    /// <param name="multiplicand">Number to be multiplied</param>
    /// <param name="multiplier">Number to multiply by</param>
    /// <returns>Product of multiplicand and multiplier</returns>
    double Multiply(double multiplicand, double multiplier);

    /// <summary>
    /// Divides the first number by the second.
    /// </summary>
    /// <param name="dividend">Dividend (number to be divided)</param>
    /// <param name="divisor">Divisor (number to divide by)</param>
    /// <returns>Quotient (dividend / divisor)</returns>
    /// <exception cref="DivideByZeroException">Thrown when divisor is zero</exception>
    double Divide(double dividend, double divisor);
}
