namespace OberbekRotation.Core
{
    /// <summary>Исходные параметры установки для вычисления τ до создания OberbeckModel.</summary>
    public sealed class OberbeckParameters
    {
        /// <summary>Задаёт массы в кг, радиус в м и момент инерции в кг·м².</summary>
        public OberbeckParameters(double mass1, double mass2, double radius, double momentOfInertia)
        {
            Mass1 = Guard.NonNegative(mass1, nameof(mass1));
            Mass2 = Guard.NonNegative(mass2, nameof(mass2));
            Radius = Guard.Positive(radius, nameof(radius));
            MomentOfInertia = Guard.Positive(momentOfInertia, nameof(momentOfInertia));
        }
        /// <summary>Первая подвешенная масса, кг.</summary>
        public double Mass1 { get; }
        /// <summary>Вторая подвешенная масса, кг.</summary>
        public double Mass2 { get; }
        /// <summary>Общий радиус канатов, м.</summary>
        public double Radius { get; }
        /// <summary>Известный момент инерции диска, кг·м².</summary>
        public double MomentOfInertia { get; }
        /// <summary>Вычисляет (m1 − m2) g R; равные массы дают ноль, m1 &lt; m2 - отрицательный момент.</summary>
        public double CalculateTorque(double gravity = SimulationConfiguration.StandardGravity)
        {
            Guard.Positive(gravity, nameof(gravity));
            return Guard.Finite((Mass1 - Mass2) * gravity * Radius, "torque");
        }
    }
}
