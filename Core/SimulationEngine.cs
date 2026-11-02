using System;
using System.Collections.Generic;

namespace OberbekRotation.Core
{
    /// <summary>Запускает интегрирование на конечном интервале и сохраняет начальную и конечную точки.</summary>
    public sealed class SimulationEngine
    {
        private readonly Rk4Solver solver;
        private readonly IPhysicsModel model;
        /// <summary>Создаёт вычислительный движок, независимый от Unity и UI.</summary>
        public SimulationEngine(Rk4Solver solver, IPhysicsModel model)
        {
            this.solver = solver ?? throw new ArgumentNullException(nameof(solver));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }
        /// <summary>Интегрирует от t0 до t1, сокращая последний шаг до точного t1.</summary>
        /// <param name="initialState">Начальный вектор; не изменяется.</param>
        /// <param name="t0">Начальное время, с.</param>
        /// <param name="t1">Конечное время, с; не меньше t0.</param>
        /// <param name="dt">Положительный шаг, с.</param>
        /// <param name="maxSteps">Ограничение числа шагов для защиты от чрезмерного выделения памяти.</param>
        public SimulationResult Run(double[] initialState, double t0, double t1, double dt, int maxSteps = SimulationConfiguration.DefaultMaxSteps)
        {
            Rk4Solver.ValidateState(model, initialState);
            Guard.Finite(t0, nameof(t0));
            Guard.Finite(t1, nameof(t1));
            Guard.Positive(dt, nameof(dt));
            if (t1 < t0) throw new ArgumentOutOfRangeException(nameof(t1));
            if (maxSteps <= 0) throw new ArgumentOutOfRangeException(nameof(maxSteps));
            double duration = Guard.NonNegative(t1 - t0, "duration");
            double stepCount = Math.Ceiling(duration / dt);
            if (stepCount > maxSteps) throw new ArgumentOutOfRangeException(nameof(dt), "Requested run exceeds maxSteps.");
            var times = new List<double> { t0 };
            var state = (double[])initialState.Clone();
            var states = new List<double[]> { state };
            double time = t0;
            for (int i = 1; time < t1; i++)
            {
                if (i > maxSteps) throw new ArgumentOutOfRangeException(nameof(dt), "Requested run exceeds maxSteps.");
                double nextTime = i >= stepCount ? t1 : Math.Min(t1, t0 + i * dt);
                if (nextTime <= time) throw new ArgumentOutOfRangeException(nameof(dt), "Step is too small at this time.");
                state = solver.Step(model, time, state, nextTime - time);
                time = nextTime;
                times.Add(time);
                states.Add(state);
            }
            return new SimulationResult(times.ToArray(), states.ToArray());
        }
    }
}
