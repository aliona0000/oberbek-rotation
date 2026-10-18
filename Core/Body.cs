namespace OberbekRotation.Core
{
    /// <summary>A point mass fixed on the rotor; positions are relative to the rotation axis.</summary>
    public sealed class Body
    {
        /// <summary>Creates a point mass in kilograms at a position in metres.</summary>
        public Body(double mass, Vector position)
        {
            Mass = Guard.Positive(mass, nameof(mass));
            Position = position;
            MomentOfInertia = Guard.NonNegative(mass * (position.X * position.X + position.Y * position.Y), "momentOfInertia");
        }
        /// <summary>Gets the mass in kg.</summary>
        public double Mass { get; }
        /// <summary>Gets the position relative to the rotor origin in m.</summary>
        public Vector Position { get; }
        /// <summary>Gets the moment of inertia about the Z axis in kg m².</summary>
        public double MomentOfInertia { get; }
    }
}
