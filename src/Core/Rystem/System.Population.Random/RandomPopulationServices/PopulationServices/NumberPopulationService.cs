using System.Security.Cryptography;

namespace System.Population.Random
{
    internal class NumberPopulationService : IRandomPopulationService
    {
        public int Priority => 1;
        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            if (options.Type.IsEnum)
            {
                var chances = Enum.GetValues(options.Type);
                var randomValue = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));
                return chances.GetValue(randomValue % chances.Length)!;
            }
            else if (options.Type == typeof(int) || options.Type == typeof(int?))
                return BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));
            else if (options.Type == typeof(uint) || options.Type == typeof(uint?))
                return BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4));
            else if (options.Type == typeof(short) || options.Type == typeof(short?))
                return BitConverter.ToInt16(RandomNumberGenerator.GetBytes(2));
            else if (options.Type == typeof(ushort) || options.Type == typeof(ushort?))
                return BitConverter.ToUInt16(RandomNumberGenerator.GetBytes(2));
            else if (options.Type == typeof(long) || options.Type == typeof(long?))
                return BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));
            else if (options.Type == typeof(ulong) || options.Type == typeof(ulong?))
                return BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
            else if (options.Type == typeof(nint) || options.Type == typeof(nint?))
                return (nint)BitConverter.ToInt16(RandomNumberGenerator.GetBytes(2));
            else if (options.Type == typeof(nuint) || options.Type == typeof(nuint?))
                return (nuint)BitConverter.ToUInt16(RandomNumberGenerator.GetBytes(2));
            else if (options.Type == typeof(float) || options.Type == typeof(float?))
                return BitConverter.ToSingle(RandomNumberGenerator.GetBytes(4));
            else if (options.Type == typeof(double) || options.Type == typeof(double?))
                return BitConverter.ToDouble(RandomNumberGenerator.GetBytes(8));
            else
                return new decimal(BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4)),
                    BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4)),
                    BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4)),
                    RandomNumberGenerator.GetInt32(4) > 1,
                    (byte)RandomNumberGenerator.GetInt32(29));
        }

        public bool IsValid(Type type)
            => type == typeof(int) || type == typeof(int?) || type == typeof(uint) || type == typeof(uint?)
                || type == typeof(short) || type == typeof(short?) || type == typeof(ushort) || type == typeof(ushort?)
                || type == typeof(long) || type == typeof(long?) || type == typeof(ulong) || type == typeof(ulong?)
                || type == typeof(nint) || type == typeof(nint?) || type == typeof(nuint) || type == typeof(nuint?)
                || type == typeof(float) || type == typeof(float?) || type == typeof(double) || type == typeof(double?)
                || type == typeof(decimal) || type == typeof(decimal?) || type.IsEnum;
    }
}
