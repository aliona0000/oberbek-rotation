using System;

namespace OberbekRotation.Core
{
    /// <summary>Immutable three-dimensional vector in a right-handed coordinate system.</summary>
    public readonly struct Vector : IEquatable<Vector>
    {
        /// <summary>Creates a vector from finite Cartesian components.</summary>
        public Vector(double x, double y, double z)
        {
            X = Guard.Finite(x, nameof(x));
            Y = Guard.Finite(y, nameof(y));
            Z = Guard.Finite(z, nameof(z));
        }
        /// <summary>Gets the X component.</summary>
        public double X { get; }
        /// <summary>Gets the Y component.</summary>
        public double Y { get; }
        /// <summary>Gets the Z component.</summary>
        public double Z { get; }
        /// <summary>Gets the zero vector.</summary>
        public static Vector Zero => default(Vector);
        /// <summary>Calculates the dot product.</summary>
        public double Dot(Vector other) => Guard.Finite(X * other.X + Y * other.Y + Z * other.Z, "result");
        /// <summary>Calculates the right-handed cross product.</summary>
        public Vector Cross(Vector other) => new Vector(Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z, X * other.Y - Y * other.X);
        /// <summary>Adds vectors component by component.</summary>
        public static Vector operator +(Vector left, Vector right) => new Vector(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        /// <summary>Subtracts vectors component by component.</summary>
        public static Vector operator -(Vector left, Vector right) => new Vector(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        /// <summary>Multiplies all components by a finite scalar.</summary>
        public static Vector operator *(Vector vector, double scalar)
        {
            Guard.Finite(scalar, nameof(scalar));
            return new Vector(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }
        /// <summary>Compares components exactly; use a tolerance for calculated values.</summary>
        public bool Equals(Vector other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Vector && Equals((Vector)obj);
        /// <inheritdoc />
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode());
    }
}
