using System;
using System.Linq;

namespace OberbekRotation.Core
{
    /// <summary>Точки траектории; Times[i] соответствует States[i]. Массивы копируются.</summary>
    public sealed class SimulationResult
    {
        private readonly double[] times;
        private readonly double[][] states;
        /// <summary>Создаёт результат с непустыми массивами и строго возрастающими временами.</summary>
        public SimulationResult(double[] times, double[][] states)
        {
            if (times == null) throw new ArgumentNullException(nameof(times));
            if (states == null) throw new ArgumentNullException(nameof(states));
            if (times.Length == 0 || times.Length != states.Length)
                throw new ArgumentException("Times and states must have the same nonzero length.");
            if (states[0] == null || states[0].Length == 0) throw new ArgumentException("State vectors cannot be empty.", nameof(states));
            StateSize = states[0].Length;
            for (int i = 0; i < times.Length; i++)
            {
                Guard.Finite(times[i], nameof(times));
                if (i > 0 && times[i] <= times[i - 1]) throw new ArgumentException("Times must strictly increase.", nameof(times));
                if (states[i] == null || states[i].Length != StateSize) throw new ArgumentException("Inconsistent state vector size.", nameof(states));
                foreach (var value in states[i]) Guard.Finite(value, nameof(states));
            }
            this.times = (double[])times.Clone();
            this.states = states.Select(s => (double[])s.Clone()).ToArray();
        }
        /// <summary>Количество записанных точек, включая начальную.</summary>
        public int Count => times.Length;
        /// <summary>Размер каждого вектора состояния.</summary>
        public int StateSize { get; }
        /// <summary>Копия времён в секундах.</summary>
        public double[] Times => (double[])times.Clone();
        /// <summary>Глубокая копия векторов; для Обербека порядок [ω, φ].</summary>
        public double[][] States => states.Select(s => (double[])s.Clone()).ToArray();
    }
}
