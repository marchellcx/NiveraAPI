namespace NiveraAPI.Random.Engines;

/// <summary>
/// Represents the default implementation of the <see cref="IRandomGenerationEngine"/> interface
/// for generating random values of different types.
/// This engine uses the <see cref="System.Random"/> class to generate random numbers
/// and provides methods to support various ranges and types.
/// </summary>
public class DefaultGenerationEngine : IRandomGenerationEngine
{
    /// <summary>
    /// Provides a static instance of the System.Random class for generating random values.
    /// The Random property is shared across methods to ensure consistent and reusable random
    /// number generation within the implementing class.
    /// </summary>
    public static System.Random Random { get; } = new();

    /// <summary>
    /// Generates a random integer value within the specified range.
    /// </summary>
    /// <param name="minValue">The minimum possible value that the generated integer can take, inclusive.</param>
    /// <param name="maxValue">The maximum possible value that the generated integer can take, exclusive.</param>
    /// <returns>A random integer value between the specified minValue (inclusive) and maxValue (exclusive).</returns>
    public int GetRandomValue(int minValue, int maxValue)
    {
        return Random.Next(minValue, maxValue);
    }

    /// <summary>
    /// Generates a random float value with a wide range, including both positive and negative values.
    /// </summary>
    /// <param name="minValue">The minimum possible value that the generated float can take.</param>
    /// <param name="maxValue">The maximum possible value that the generated float can take.</param>
    /// <returns>A random float value between the specified minValue and maxValue.</returns>
    public float GetRandomValue(float minValue, float maxValue)
    {
        double num = Random.NextDouble() * 2.0 - 1.0;
        double num2 = Math.Pow(2.0, Random.Next(-126, 128));
        
        return (float)(num * num2);
    }

    /// <summary>
    /// Generates a random byte value within the specified range.
    /// </summary>
    /// <param name="minValue">The inclusive lower bound of the byte value to be generated.</param>
    /// <param name="maxValue">The exclusive upper bound of the byte value to be generated.</param>
    /// <returns>A random byte value within the range [minValue, maxValue).</returns>
    public byte GetRandomValue(byte minValue, byte maxValue)
    {
        return (byte)Random.Next(minValue, maxValue);
    }
}