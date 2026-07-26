namespace NiveraAPI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class LoadAttribute : Attribute
{
    public LoadPriority Priority { get; set; } = LoadPriority.Normal;
}