using System.Security.Cryptography;

namespace System.Population.Random
{
    internal class RangePopulationService : IRandomPopulationService
    {
        public int Priority => 1;
        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            var firstNumber = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));
            var secondNumber = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));
            if (firstNumber < 0)
                firstNumber *= -1;
            if (secondNumber < 0)
                secondNumber *= -1;
            if (firstNumber > secondNumber)
                (secondNumber, firstNumber) = (firstNumber, secondNumber);
            if (firstNumber == secondNumber)
                secondNumber++;
            return new Range(new Index(firstNumber), new Index(secondNumber));
        }

        public bool IsValid(Type type)
            => type == typeof(Range) || type == typeof(Range?);
    }
}
