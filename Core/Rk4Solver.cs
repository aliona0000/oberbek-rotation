using System;

namespace OberbekRotation.Core
{
    /// <summary>Классический явный метод Рунге-Кутта четвёртого порядка для гладких систем ОДУ.</summary>
    public sealed class Rk4Solver
    {
        /// <summary>Выполняет один шаг RK4 и возвращает новый вектор; исходный массив не изменяется.</summary>
        public double[] Step(IPhysicsModel model, double time, double[] state, double dt)
        {
            ValidateState(model, state);
            Guard.Finite(time, nameof(time));
            Guard.Positive(dt, nameof(dt));
            double end = Guard.Finite(time + dt, "endTime");
            if (end <= time) throw new ArgumentOutOfRangeException(nameof(dt), "Step is too small at this time.");
            double half = Guard.Finite(time + dt / 2, "halfTime");
            var k1 = Derivatives(model, time, state);
            var k2 = Derivatives(model, half, Add(state, k1, dt / 2));
            var k3 = Derivatives(model, half, Add(state, k2, dt / 2));
            var k4 = Derivatives(model, end, Add(state, k3, dt));
            var next = new double[state.Length];
            for (int i = 0; i < next.Length; i++)
                next[i] = Guard.Finite(state[i] + dt * (k1[i] / 6 + k2[i] / 3 + k3[i] / 3 + k4[i] / 6), "result");
            return next;
        }
        internal static void ValidateState(IPhysicsModel model, double[] state)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (model.StateSize <= 0 || state.Length != model.StateSize)
                throw new ArgumentException("State length must match positive model.StateSize.", nameof(state));
            foreach (double value in state) Guard.Finite(value, nameof(state));
        }
        private static double[] Derivatives(IPhysicsModel model, double time, double[] state)
        {
            var result = model.GetDerivatives(time, (double[])state.Clone());
            if (result == null || result.Length != state.Length)
                throw new InvalidOperationException("Model returned an invalid derivative vector.");
            foreach (double value in result)
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new InvalidOperationException("Model returned a non-finite derivative.");
            return (double[])result.Clone();
        }
        private static double[] Add(double[] state, double[] slope, double step)
        {
            var result = new double[state.Length];
            for (int i = 0; i < result.Length; i++) result[i] = Guard.Finite(state[i] + step * slope[i], "intermediateState");
            return result;
        }
    }
}
