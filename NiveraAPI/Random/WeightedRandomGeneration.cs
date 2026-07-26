using NiveraAPI.Random.Engines;
using NiveraAPI.Utilities;

namespace NiveraAPI.Random;

/// <summary>
/// Provides a mechanism to generate weighted random outcomes based on specified probabilities.
/// This class enables selecting objects or boolean values, ensuring the likelihood of selection
/// is proportional to their assigned weights.
/// </summary>
public class WeightedRandomGeneration
{
    private readonly IRandomGenerationEngine _randomGenerationEngine;

    /// <summary>
    /// Provides a default instance of the <see cref="WeightedRandomGeneration"/> class
    /// configured with the <see cref="DefaultGenerationEngine"/>.
    /// This property ensures a ready-to-use implementation for generating weighted random values
    /// without requiring additional setup or initialization.
    /// </summary>
    public static WeightedRandomGeneration Default { get; } = new(Singleton<DefaultGenerationEngine>.Value);

    /// <summary>
    /// Specifies whether the sum of the weights for the provided items must strictly equal 100
    /// when selecting an item using weighted random generation.
    /// If set to <c>true</c>, an exception is thrown if the sum does not equal 100, enforcing strict validation.
    /// If set to <c>false</c>, no validation is performed on the sum of the weights.
    /// </summary>
    public bool EnsureCorrectSum { get; set; }
    
    /// <summary>
    /// Creates a new instance of the <see cref="WeightedRandomGeneration"/> class.
    /// </summary>
    public WeightedRandomGeneration(IRandomGenerationEngine randomGenerationEngine)
    {
        _randomGenerationEngine = randomGenerationEngine ?? throw new ArgumentNullException(nameof(randomGenerationEngine));
    }

    /// <summary>
    /// Generates a random boolean value based on the specified chance of being true.
    /// </summary>
    /// <param name="trueChance">
    /// The percentage chance of the returned value being true.
    /// The value must be between 0 and 100. The default value is 50.
    /// </param>
    /// <returns>
    /// A boolean value where the likelihood of it being true is determined by the specified <paramref name="trueChance"/>.
    /// </returns>
    public bool GetBool(float trueChance = 50)
        => PickObject(x => x ? trueChance : (100 - trueChance), true, false);

    /// <summary>
    /// Selects a single object from the provided list based on the weights determined by the given weight selector function.
    /// </summary>
    /// <param name="weightPicker">
    /// A function that determines the weight of each object in the collection.
    /// The weight must be represented as an integer.
    /// </param>
    /// <param name="objects">
    /// The collection of objects to select from.
    /// This parameter must not be null, and the collection must contain at least one object.
    /// </param>
    /// <typeparam name="TObject">
    /// The type of the objects in the collection.
    /// </typeparam>
    /// <returns>
    /// A randomly selected object from the collection, where the likelihood of selection is determined by the weights computed using <paramref name="weightPicker"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="objects"/> parameter is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection of objects is empty, or when the sum of weights across all objects is not equal to 100 and <c>EnsureCorrectSum</c> is set to true.
    /// </exception>
    public TObject PickObject<TObject>(Func<TObject, float> weightPicker, params TObject[] objects)
    {
        if (objects == null)
            throw new ArgumentNullException(nameof(objects));
        
        if (objects.Length < 1)
            throw new InvalidOperationException("Cannot pick from an empty list!");

        var num = objects.Sum(weightPicker);

        if (num is < 100f or > 100f && EnsureCorrectSum)
            throw new InvalidOperationException("The sum of the provided list is not equal to a hundred.");

        var num2 = 0f;
        var randomValue = _randomGenerationEngine.GetRandomValue(0, num);
        
        for (var i = 0; i < objects.Length; i++)
        {
            var item = objects[i];
            var num3 = weightPicker(item);
            
            for (var x = num2; x < num3 + num2; x++)
            {
                if (x >= randomValue)
                {
                    return item;
                }
            }
            
            num2 += num3;
        }

        return objects[0];
    }
}