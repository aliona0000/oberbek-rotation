using System;
using System.Collections.Generic;
using System.Globalization;

namespace OberbekRotation.Core.Presentation
{
    /// <summary>Описание модели маятника Обербека для внешнего мира: параметры, величины, запуск.</summary>
    public sealed class OberbeckModelDescriptor : IModelDescriptor
    {
        private static readonly ParameterInfo[] ParameterList =
        {
            new ParameterInfo("mass1",           "Mass 1",                 "kg",    0.200, 0.001, 100),
            new ParameterInfo("mass2",           "Mass 2",                 "kg",    0.150, 0.001, 100),
            new ParameterInfo("radius",          "Radius",                 "m",     0.050, 0.001, 10),
            new ParameterInfo("momentOfInertia", "Moment of inertia",      "kg·m²", 0.010, 1e-6,  100),
            new ParameterInfo("gravity",         "Acceleration of gravity","m/s²",  9.81,  1e-6,  100),
            new ParameterInfo("timeStep",        "Integration step",       "s",     0.01,  1e-6,  1.0),
            new ParameterInfo("endTime",         "Simulation time",        "s",     2.0,   0.001, 1000),
        };

        // порядок совпадает с OberbeckSession.BuildValues
        private static readonly OutputInfo[] OutputList =
        {
            new OutputInfo("time",   "Time",                 "s"),
            new OutputInfo("omega",  "Angular velocity",     "rad/s"),
            new OutputInfo("phi",    "Rotation angle",       "rad"),
            new OutputInfo("path",   "Load travel",          "m"),
            new OutputInfo("speed",  "Load speed",           "m/s"),
            new OutputInfo("torque", "Torque",               "N·m"),
            new OutputInfo("alpha",  "Angular acceleration", "rad/s²"),
        };

        /// <summary>Название физической модели.</summary>
        public string Name { get { return "Oberbeck pendulum"; } }
        /// <summary>Описания входных параметров.</summary>
        public IReadOnlyList<ParameterInfo> Parameters { get { return Array.AsReadOnly(ParameterList); } }
        /// <summary>Описания выходных величин в порядке значений кадра.</summary>
        public IReadOnlyList<OutputInfo> Outputs { get { return Array.AsReadOnly(OutputList); } }

        /// <summary>Проверяет параметры, выполняет расчёт RK4 и создаёт сессию показа.</summary>
        public ISimulationSession Start(IReadOnlyDictionary<string, double> values)
        {
            double mass1 = Read(values, "mass1");
            double mass2 = Read(values, "mass2");
            double radius = Read(values, "radius");
            double inertia = Read(values, "momentOfInertia");
            double gravity = Read(values, "gravity");
            double timeStep = Read(values, "timeStep");
            double endTime = Read(values, "endTime");

            var configuration = new SimulationConfiguration(gravity: gravity, timeStep: timeStep);
            var parameters = new OberbeckParameters(mass1: mass1, mass2: mass2, radius: radius, momentOfInertia: inertia);
            var request = new SimulationRequest(parameters, endTime: endTime, configuration: configuration);
            SimulationResult result = new OberbeckSimulation().Run(request);

            return new OberbeckSession(parameters, gravity, request, result, Outputs);
        }

        private static double Read(IReadOnlyDictionary<string, double> values, string id)
        {
            ParameterInfo info = null;
            foreach (var p in ParameterList)
            {
                if (p.Id == id) { info = p; break; }
            }
            if (info == null) throw new ArgumentException("Unknown parameter: " + id);

            double v;
            if (values == null || !values.TryGetValue(id, out v))
                v = info.DefaultValue;

            if (double.IsNaN(v) || double.IsInfinity(v) || v < info.MinValue || v > info.MaxValue)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "{0} must be between {1} and {2} {3}.", info.DisplayName, info.MinValue, info.MaxValue, info.Unit));
            }
            return v;
        }
    }
}
