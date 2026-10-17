using System;
using OberbekRotation.Core;

namespace OberbekRotation.Export
{
    /// <summary>Named experiment with complete model, configuration and recorded trajectory.</summary>
    public sealed class Experiment
    {
        /// <summary>Creates an immutable snapshot of a two-component Oberbeck trajectory.</summary>
        public Experiment(string name, SimulationRequest input, SimulationResult result)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Model = input.CreateModel();
            Result = result ?? throw new ArgumentNullException(nameof(result));
            if (result.StateSize != Model.StateSize) throw new ArgumentException("Expected states [omega, phi].", nameof(result));
            var times = result.Times;
            var states = result.States;
            if (times[0] != input.StartTime || times[times.Length - 1] != input.EndTime ||
                states[0][0] != input.InitialOmega || states[0][1] != input.InitialPhi)
                throw new ArgumentException("Result boundaries or initial state do not match the input.", nameof(result));
        }
        /// <summary>Gets all validated input parameters, including masses and initial conditions.</summary>
        public SimulationRequest Input { get; }
        /// <summary>Gets the free-form name.</summary>
        public string Name { get; }
        /// <summary>Gets the model with inertia and torque.</summary>
        public OberbeckModel Model { get; }
        /// <summary>Gets gravity and default sampling settings.</summary>
        public SimulationConfiguration Configuration => Input.Configuration;
        /// <summary>Gets recorded times and [omega, phi] states.</summary>
        public SimulationResult Result { get; }
    }
}
