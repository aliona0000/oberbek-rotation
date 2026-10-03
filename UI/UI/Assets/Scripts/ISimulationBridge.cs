// Assets/Scripts/ISimulationBridge.cs
using System.Collections.Generic;

public class ParameterDescriptor
{
    public string Id;
    public string DisplayName;
    public string Unit;
    public double DefaultValue;
    public double MinValue;
    public double MaxValue;
}

public class OutputDescriptor
{
    public string Id;
    public string DisplayName;
    public string Unit;
}

public interface ISimulationBridge
{
    IReadOnlyList<ParameterDescriptor> GetInputParameters();
    IReadOnlyList<OutputDescriptor> GetOutputValues();
    void SetParameter(string id, double value);
    double GetOutput(string id);
    void Step(double deltaTime);
}