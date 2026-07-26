namespace NiveraAPI.Random;

/// <summary>
/// Defines the contract for a random generation engine that is capable of producing random
/// values for various numeric types, such as integers, floats, and bytes, within a specified range.
/// </summary>
public interface IRandomGenerationEngine
{
    /// <summary>
    /// Generates a random integer value within the specified range.
    /// </summary>
    /// <param name="minValue">The minimum possible value that the generated integer can take, inclusive.</param>
    /// <param name="maxValue">The maximum possible value that the generated integer can take, exclusive.</param>
    /// <returns>A random integer value between the specified minValue (inclusive) and maxValue (exclusive).</returns>
    int GetRandomValue(int minValue, int maxValue);

    /// <summary>
    /// Generates a random float value within the specified range.
    /// </summary>
    /// <param name="minValue">The inclusive lower bound of the value to be generated.</param>
    /// <param name="maxValue">The exclusive upper bound of the value to be generated.</param>
    /// <returns>A random float value within the range [minValue, maxValue).</returns>
    float GetRandomValue(float minValue, float maxValue);

    /// <summary>
    /// Generates a random value within the specified range.
    /// </summary>
    /// <param name="minValue">The inclusive lower bound of the value to be generated.</param>
    /// <param name="maxValue">The exclusive upper bound of the value to be generated.</param>
    /// <returns>A random value within the range [minValue, maxValue).</returns>
    byte GetRandomValue(byte minValue, byte maxValue);
}