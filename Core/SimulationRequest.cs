using System;

namespace OberbekRotation.Core
{
    /// <summary>Проверенные входные данные одного запуска, передаваемые из ViewModel в Core.</summary>
    public sealed class SimulationRequest
    {
        /// <summary>Создаёт запрос с параметрами установки, интервалом и начальными условиями в SI.</summary>
        public SimulationRequest(OberbeckParameters parameters, double endTime,
            SimulationConfiguration configuration = null, double initialOmega = 0,
            double initialPhi = 0, double startTime = 0, int maxSteps = SimulationConfiguration.DefaultMaxSteps)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Configuration = configuration ?? new SimulationConfiguration();
            InitialOmega = Guard.Finite(initialOmega, nameof(initialOmega));
            InitialPhi = Guard.Finite(initialPhi, nameof(initialPhi));
            StartTime = Guard.Finite(startTime, nameof(startTime));
            EndTime = Guard.Finite(endTime, nameof(endTime));
            if (endTime < startTime) throw new ArgumentOutOfRangeException(nameof(endTime), "End time must be at least start time.");
            if (maxSteps <= 0) throw new ArgumentOutOfRangeException(nameof(maxSteps));
            MaxSteps = maxSteps;
            double duration = Guard.NonNegative(endTime - startTime, "duration");
            if (Math.Ceiling(duration / Configuration.TimeStep) > maxSteps)
                throw new ArgumentOutOfRangeException(nameof(endTime), "Requested run exceeds maxSteps.");
            if (duration > 0 && startTime + Math.Min(duration, Configuration.TimeStep) <= startTime)
                throw new ArgumentOutOfRangeException(nameof(configuration), "Step is too small at this start time.");
            // Проверяем также переполнение при вычислении τ и α до запуска цикла.
            CreateModel();
        }
        /// <summary>Параметры установки.</summary>
        public OberbeckParameters Parameters { get; }
        /// <summary>Ускорение свободного падения и шаг интегрирования.</summary>
        public SimulationConfiguration Configuration { get; }
        /// <summary>Начальная угловая скорость, рад/с.</summary>
        public double InitialOmega { get; }
        /// <summary>Начальный угол, рад.</summary>
        public double InitialPhi { get; }
        /// <summary>Начальное время, с.</summary>
        public double StartTime { get; }
        /// <summary>Конечное время, с.</summary>
        public double EndTime { get; }
        /// <summary>Максимальное число шагов.</summary>
        public int MaxSteps { get; }
        /// <summary>Вычисляет τ вне физической модели и передаёт I и τ её конструктору.</summary>
        public OberbeckModel CreateModel() => new OberbeckModel(Parameters.MomentOfInertia,
            Parameters.CalculateTorque(Configuration.Gravity));
    }
}
