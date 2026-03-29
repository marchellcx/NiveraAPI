namespace NiveraAPI.Utilities
{
    /// <summary>
    /// Provides static methods for generating random numbers of various numeric types within specified ranges.
    /// </summary>
    public static class StaticRandom
    {
        private static volatile Random random = new();

        /// <summary>
        /// Gets a random Boolean value that is either true or false.
        /// </summary>
        public static bool Bool => random.Next(1) == 0;
        
        /// <summary>
        /// Gets a random byte value.
        /// </summary>
        public static byte Byte => GetByte(byte.MinValue, byte.MaxValue);
        
        /// <summary>
        /// Gets a random signed byte value.
        /// </summary>
        public static sbyte SByte => GetSByte(sbyte.MinValue, sbyte.MaxValue);
        
        /// <summary>
        /// Gets a random 16-bit signed integer.
        /// </summary>
        public static short Short => GetShort(short.MinValue, short.MaxValue);
        
        /// <summary>
        /// Gets a random unsigned 16-bit integer.
        /// </summary>
        public static ushort UShort => GetUShort(ushort.MinValue, ushort.MaxValue);
        
        /// <summary>
        /// Gets a random integer.
        /// </summary>
        public static int Int => GetInt(int.MinValue, int.MaxValue);
        
        /// <summary>
        /// Gets a random unsigned integer.
        /// </summary>
        public static uint UInt => GetUInt(uint.MinValue, uint.MaxValue);
        
        /// <summary>
        /// Gets a random 64-bit signed integer.
        /// </summary>
        public static long Long => GetLong(long.MinValue, long.MaxValue);
        
        /// <summary>
        /// Gets a random unsigned 64-bit integer.
        /// </summary>
        public static ulong ULong => GetULong(ulong.MinValue, ulong.MaxValue);
        
        /// <summary>
        /// Gets a random floating-point number.
        /// </summary>
        public static float Float => GetFloat(float.MinValue, float.MaxValue);

        /// <summary>
        /// Gets a random double-precision floating-point number.
        /// </summary>
        public static float PositiveFloat => GetFloat(0f, float.MaxValue);
        
        /// <summary>
        /// Gets a random floating-point number.
        /// </summary>
        public static double Double => GetDouble(double.MinValue, double.MaxValue);
        
        /// <summary>
        /// Gets a random double-precision floating-point number.
        /// </summary>
        public static double PositiveDouble => GetDouble(0d, double.MaxValue);
        
        /// <summary>
        /// Generates a random byte value within the specified range.
        /// </summary>
        /// <remarks>Both minValue and maxValue must be within the valid range for byte values (0 to 255).
        /// If minValue equals maxValue, the method will always return minValue.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random byte value to generate. Must be less than or equal to maxValue.</param>
        /// <param name="maxValue">The exclusive upper bound of the random byte value to generate. Must be greater than minValue.</param>
        /// <returns>A random byte value greater than or equal to minValue and less than maxValue.</returns>
        public static byte GetByte(byte minValue, byte maxValue)
        {
            return (byte)random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Generates a random signed byte value within the specified range.
        /// </summary>
        /// <remarks>If <paramref name="minValue"/> is not less than <paramref name="maxValue"/>, an <see
        /// cref="ArgumentOutOfRangeException"/> is thrown. The method uses a shared random number generator and is not
        /// thread-safe.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random value to generate. Must be less than <paramref name="maxValue"/>.</param>
        /// <param name="maxValue">The exclusive upper bound of the random value to generate. Must be greater than <paramref name="minValue"/>.</param>
        /// <returns>A random signed byte value greater than or equal to <paramref name="minValue"/> and less than <paramref
        /// name="maxValue"/>.</returns>
        public static sbyte GetSByte(sbyte minValue, sbyte maxValue)
        {
            return (sbyte)random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a random 16-bit signed integer that is greater than or equal to the specified minimum value and less
        /// than the specified maximum value.
        /// </summary>
        /// <remarks>If <paramref name="minValue"/> is equal to or greater than <paramref
        /// name="maxValue"/>, an exception is thrown.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to be generated. Must be less than <paramref
        /// name="maxValue"/>.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than <paramref
        /// name="minValue"/>.</param>
        /// <returns>A random 16-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref
        /// name="maxValue"/>.</returns>
        public static short GetShort(short minValue, short maxValue)
        {
            return (short)random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Generates a random unsigned 16-bit integer within the specified range.
        /// </summary>
        /// <remarks>If <paramref name="minValue"/> is not less than <paramref name="maxValue"/>, an <see
        /// cref="ArgumentOutOfRangeException"/> is thrown. This method uses a shared random number generator and is not
        /// thread-safe.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to generate. Must be less than <paramref name="maxValue"/>.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to generate. Must be greater than <paramref
        /// name="minValue"/>.</param>
        /// <returns>A random <see cref="ushort"/> value greater than or equal to <paramref name="minValue"/> and less than
        /// <paramref name="maxValue"/>.</returns>
        public static ushort GetUShort(ushort minValue, ushort maxValue)
        {
            return (ushort)random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Generates a random integer within the specified range.
        /// </summary>
        /// <remarks>If minValue is not less than maxValue, the method throws an
        /// ArgumentOutOfRangeException. The distribution of generated values is uniform within the specified
        /// range.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to generate. Must be less than maxValue.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to generate. Must be greater than minValue.</param>
        /// <returns>A random integer greater than or equal to minValue and less than maxValue.</returns>
        public static int GetInt(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Generates a random unsigned integer within the specified range.
        /// </summary>
        /// <remarks>If minValue is not less than maxValue, the method throws an
        /// ArgumentOutOfRangeException. The distribution of generated values is uniform within the specified
        /// range.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to generate. Must be less than maxValue.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to generate. Must be greater than minValue.</param>
        /// <returns>A random unsigned integer greater than or equal to minValue and less than maxValue.</returns>
        public static uint GetUInt(uint minValue, uint maxValue)
        {
            return (uint)random.Next((int)minValue, (int)maxValue);
        }

        /// <summary>
        /// Generates a random 64-bit signed integer within the specified range.
        /// </summary>
        /// <remarks>This method uses a cryptographically strong random number generator to produce the
        /// result. The range specified by minValue and maxValue must be valid; otherwise, the behavior is
        /// undefined.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to be generated. Must be less than maxValue.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than minValue.</param>
        /// <returns>A random 64-bit signed integer greater than or equal to minValue and less than maxValue.</returns>
        public static long GetLong(long minValue, long maxValue)
        {
            var buffer = new byte[8];

            random.NextBytes(buffer);

            var result = BitConverter.ToInt64(buffer, 0);
            return (result % (maxValue - minValue)) + minValue;
        }

        /// <summary>
        /// Generates a random unsigned long integer within the specified range.
        /// </summary>
        /// <remarks>Use this method to obtain a uniformly distributed random unsigned long integer within
        /// the specified range. If <paramref name="minValue"/> equals <paramref name="maxValue"/>, the method always
        /// returns <paramref name="minValue"/>.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to be generated. Must be less than or equal to <paramref
        /// name="maxValue"/>.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than <paramref
        /// name="minValue"/>.</param>
        /// <returns>A random unsigned long integer greater than or equal to <paramref name="minValue"/> and less than <paramref
        /// name="maxValue"/>.</returns>
        public static ulong GetULong(ulong minValue, ulong maxValue)
        {
            var buffer = new byte[8];

            random.NextBytes(buffer);

            var result = BitConverter.ToUInt64(buffer, 0);
            return (result % (maxValue - minValue)) + minValue;
        }

        /// <summary>
        /// Generates a random floating-point number that is greater than or equal to the specified minimum value and
        /// less than the specified maximum value.
        /// </summary>
        /// <remarks>The method uses a uniform distribution to generate the random value. If minValue is
        /// not less than maxValue, the result may be unexpected.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to generate. Must be less than maxValue.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to generate. Must be greater than minValue.</param>
        /// <returns>A random float value greater than or equal to minValue and less than maxValue.</returns>
        public static float GetFloat(float minValue, float maxValue)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }

        /// <summary>
        /// Generates a random double-precision floating-point number within the specified range.
        /// </summary>
        /// <remarks>The method uses a uniform distribution to generate the random number. Ensure that
        /// <paramref name="minValue"/> is less than <paramref name="maxValue"/> to avoid unexpected results.</remarks>
        /// <param name="minValue">The inclusive lower bound of the random number to be generated. Must be less than <paramref
        /// name="maxValue"/>.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than <paramref
        /// name="minValue"/>.</param>
        /// <returns>A double-precision floating-point number greater than or equal to <paramref name="minValue"/> and less than
        /// <paramref name="maxValue"/>.</returns>
        public static double GetDouble(double minValue, double maxValue)
        {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified count.
        /// </summary>
        /// <param name="count">The exclusive upper bound for the random index to generate. Must be greater than or equal to 0.</param>
        /// <returns>A random integer greater than or equal to 0 and less than the specified count.</returns>
        public static int GetIndex(int count)
        {
            return GetInt(0, count);
        }

        /// <summary>
        /// Returns a pseudo-random integer within a specified range, starting from the given index.
        /// </summary>
        /// <param name="startIndex">The inclusive lower bound of the range from which to generate the random integer.</param>
        /// <param name="count">The number of consecutive integers in the range. Must be greater than zero.</param>
        /// <returns>A pseudo-random integer greater than or equal to startIndex and less than startIndex plus count.</returns>
        public static int GetIndex(int startIndex, int count)
        {
            return GetInt(startIndex, count);
        }
    }
}
