using System;
using System.Globalization;
using System.IO;
using System.Linq;
using OberbekRotation.Core;
using OberbekRotation.Export;

// Этот же вызов может делать ViewModel. Core не зависит от Unity или элементов UI.
string outputDirectory = args.Length == 0 ? "artifacts/example" : args[0];
Directory.CreateDirectory(outputDirectory);
var configuration = new SimulationConfiguration(gravity: 9.81, timeStep: 0.01);
var parameters = new OberbeckParameters(mass1: 0.200, mass2: 0.150, radius: 0.050, momentOfInertia: 0.010);
var input = new SimulationRequest(parameters, endTime: 2, configuration: configuration);
var result = new OberbeckSimulation().Run(input);
var experiment = new Experiment("Обербек - пример из методички", input, result);
string jsonPath = Path.Combine(outputDirectory, "oberbeck.json");
ExperimentFile.Save(jsonPath, experiment);
ExperimentFile.Save(Path.Combine(outputDirectory, "oberbeck.csv"), experiment);
var restored = ExperimentFile.Load(jsonPath);
var last = restored.Result.States.Last();
Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
    "tau={0:F6} N*m; alpha={1:F6} rad/s^2; omega(2)={2:F6} rad/s; phi(2)={3:F6} rad; points={4}",
    restored.Model.Torque, restored.Model.AngularAcceleration, last[0], last[1], restored.Result.Count));
if (Math.Abs(last[0] - 4.905) > 1e-10 || Math.Abs(last[1] - 4.905) > 1e-10)
    throw new InvalidOperationException("Lab example does not match the analytic reference.");
Console.WriteLine("JSON and CSV saved to " + Path.GetFullPath(outputDirectory));
