namespace NiveraAPI.Random;

public struct WeightedObject
{
    public int Weight { get; set; }

    public object Object { get; }

    public WeightedObject(int weight, object obj)
    {
        Weight = weight;
        Object = obj;
    }

    public static WeightedObject Create(object obj, int weight)
    {
        return new WeightedObject(weight, obj);
    }
}
