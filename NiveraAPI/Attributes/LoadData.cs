using System.Reflection;

namespace NiveraAPI.Attributes;

public struct LoadData
{
    public LoadPriority priority;

    public MethodBase target;

    public LoadData(MethodBase target, LoadPriority priority)
    {
        this.priority = priority;
        this.target = target;
    }
}