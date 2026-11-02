using System;

namespace OberbekRotation.Core
{
    /// <summary>Точка входа для ViewModel: проверенные входные параметры → массивы данных для графиков.</summary>
    public sealed class OberbeckSimulation
    {
        /// <summary>Запускает RK4; результат содержит время и state = [ω, φ], включая начальную точку.</summary>
        public SimulationResult Run(SimulationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var engine = new SimulationEngine(new Rk4Solver(), request.CreateModel());
            return engine.Run(new[] { request.InitialOmega, request.InitialPhi },
                request.StartTime, request.EndTime, request.Configuration.TimeStep, request.MaxSteps);
        }
    }
}
