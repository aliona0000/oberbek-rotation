namespace OberbekRotation.Core
{
    /// <summary>A force and its application point, both expressed in the same coordinate system.</summary>
    public sealed class Force
    {
        /// <summary>Creates a force in newtons applied at a position in metres.</summary>
        public Force(Vector value, Vector applicationPoint)
        {
            Value = value;
            ApplicationPoint = applicationPoint;
        }
        /// <summary>Gets the force vector in N.</summary>
        public Vector Value { get; }
        /// <summary>Gets the application point in m.</summary>
        public Vector ApplicationPoint { get; }
        /// <summary>Calculates torque about the supplied origin in N m.</summary>
        public Vector TorqueAbout(Vector origin) => (ApplicationPoint - origin).Cross(Value);
    }
}
