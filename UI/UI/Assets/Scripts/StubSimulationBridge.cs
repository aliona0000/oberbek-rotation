// Assets/Scripts/StubSimulationBridge.cs
using System.Collections.Generic;
using UnityEngine;

public class StubSimulationBridge : ISimulationBridge
{
    private readonly Dictionary<string, double> _params = new Dictionary<string, double>();
    private double _omega;
    private double _phi;
    private double _time;

    public StubSimulationBridge()
    {
        _params["mass"] = 0.5;
        _params["radius"] = 0.1;
        _params["alpha"] = 1.5; // рад/с² — для теста
        _omega = 0;
        _phi = 0;
        _time = 0;
    }

    public IReadOnlyList<ParameterDescriptor> GetInputParameters()
    {
        return new List<ParameterDescriptor>
        {
            new ParameterDescriptor { Id = "mass",   DisplayName = "Масса груза",        Unit = "кг",  DefaultValue = 0.5, MinValue = 0.01, MaxValue = 10 },
            new ParameterDescriptor { Id = "radius", DisplayName = "Радиус шкива",       Unit = "м",   DefaultValue = 0.1, MinValue = 0.01, MaxValue = 1  },
            new ParameterDescriptor { Id = "alpha",  DisplayName = "Угловое ускорение",  Unit = "рад/с²", DefaultValue = 1.5, MinValue = 0, MaxValue = 10 },
        };
    }

    public IReadOnlyList<OutputDescriptor> GetOutputValues()
    {
        return new List<OutputDescriptor>
        {
            new OutputDescriptor { Id = "time",  DisplayName = "Время",             Unit = "с"    },
            new OutputDescriptor { Id = "omega", DisplayName = "Угловая скорость",  Unit = "рад/с" },
            new OutputDescriptor { Id = "phi",   DisplayName = "Угол",              Unit = "рад"  },
        };
    }

    public void SetParameter(string id, double value)
    {
        if (_params.ContainsKey(id)) _params[id] = value;
    }

    public double GetOutput(string id)
    {
        switch (id)
        {
            case "time": return _time;
            case "omega": return _omega;
            case "phi": return _phi;
            default: return 0;
        }
    }

    public void Step(double deltaTime)
    {
        double alpha = _params["alpha"];
        _omega += alpha * deltaTime;
        _phi += _omega * deltaTime;
        _time += deltaTime;
    }
}