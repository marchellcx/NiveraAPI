namespace NiveraAPI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class UnloadAttribute : Attribute
{
    public LoadPriority Priority { get; set; } = LoadPriority.Normal;

}