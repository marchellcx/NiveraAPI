namespace NiveraAPI.Commands.Attributes;

[AttributeUsage(AttributeTargets.Method, 
    AllowMultiple = true, 
    Inherited = false)]
public class ParameterSubAttribute : Attribute
{
    
}