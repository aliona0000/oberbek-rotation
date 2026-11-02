namespace OberbekRotation.Core
{
    /// <summary>Physical constants and sampling defaults, separate from the equations.</summary>
    public sealed class SimulationConfiguration
    {
        /// <summary>Default terrestrial gravitational acceleration in m/s².</summary>
        public const double StandardGravity = 9.81;
        /// <summary>Default output sampling interval in seconds.</summary>
        public const double DefaultTimeStep = 0.01;
        /// <summary>Default maximum number of integration steps per run.</summary>
        public const int DefaultMaxSteps = 1000000;
        /// <summary>Creates validated simulation settings.</summary>
        public SimulationConfiguration(double gravity = StandardGravity, double timeStep = DefaultTimeStep)
        {
            Gravity = Guard.Positive(gravity, nameof(gravity));
            TimeStep = Guard.Positive(timeStep, nameof(timeStep));
        }
        /// <summary>Gets gravitational acceleration in m/s².</summary>
        public double Gravity { get; }
        /// <summary>Gets the default sampling interval in s.</summary>
        public double TimeStep { get; }
    }
}
