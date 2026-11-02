using System;

namespace OberbekRotation.Core
{
    /// <summary>Упрощённая модель из методички: постоянный момент τ, state = [ω, φ].</summary>
    /// <remarks>Нить нерастяжима и не проскальзывает, диск твёрдый, трением пренебрегаем.
    /// Момент вычисляется до создания модели: τ = (m1 − m2) g R.</remarks>
    public sealed class OberbeckModel : IPhysicsModel
    {
        /// <summary>Создаёт модель по моменту инерции (кг·м²) и моменту силы (Н·м).</summary>
        public OberbeckModel(double momentOfInertia, double torque)
        {
            MomentOfInertia = Guard.Positive(momentOfInertia, nameof(momentOfInertia));
            Torque = Guard.Finite(torque, nameof(torque));
            AngularAcceleration = Guard.Finite(torque / momentOfInertia, "angularAcceleration");
        }
        /// <inheritdoc />
        public string Name => "Обербек";
        /// <inheritdoc />
        public int StateSize => 2;
        /// <summary>Момент инерции диска, кг·м².</summary>
        public double MomentOfInertia { get; }
        /// <summary>Постоянный знаковый момент силы, Н·м.</summary>
        public double Torque { get; }
        /// <summary>Угловое ускорение τ/I, рад/с².</summary>
        public double AngularAcceleration { get; }
        /// <summary>Возвращает [dω/dt, dφ/dt] = [τ/I, ω].</summary>
        public double[] GetDerivatives(double time, double[] state)
        {
            Guard.Finite(time, nameof(time));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Length != StateSize) throw new ArgumentException("Expected state = [omega, phi].", nameof(state));
            Guard.Finite(state[0], nameof(state));
            Guard.Finite(state[1], nameof(state));
            return new[] { AngularAcceleration, state[0] };
        }
    }
}
