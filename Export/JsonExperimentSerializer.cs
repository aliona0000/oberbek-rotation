using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OberbekRotation.Core;

namespace OberbekRotation.Export
{
    /// <summary>Versioned JSON contract, using explicit fields without runtime type activation.</summary>
    public sealed class JsonExperimentSerializer
    {
        /// <summary>Current file schema version.</summary>
        public const int SchemaVersion = 1;
        /// <summary>Serializes the model, configuration, times and [omega, phi] states.</summary>
        public string Serialize(Experiment experiment)
        {
            if (experiment == null) throw new ArgumentNullException(nameof(experiment));
            var root = new JObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["name"] = experiment.Name,
                ["modelType"] = "Oberbeck",
                ["input"] = new JObject
                {
                    ["mass1"] = experiment.Input.Parameters.Mass1,
                    ["mass2"] = experiment.Input.Parameters.Mass2,
                    ["radius"] = experiment.Input.Parameters.Radius,
                    ["momentOfInertia"] = experiment.Input.Parameters.MomentOfInertia,
                    ["initialOmega"] = experiment.Input.InitialOmega,
                    ["initialPhi"] = experiment.Input.InitialPhi,
                    ["startTime"] = experiment.Input.StartTime,
                    ["endTime"] = experiment.Input.EndTime,
                    ["maxSteps"] = experiment.Input.MaxSteps
                },
                ["configuration"] = new JObject
                {
                    ["gravity"] = experiment.Configuration.Gravity, ["timeStep"] = experiment.Configuration.TimeStep
                },
                ["times"] = new JArray(experiment.Result.Times),
                ["states"] = new JArray(experiment.Result.States.Select(s => new JArray(s)))
            };
            return root.ToString(Formatting.Indented);
        }
        /// <summary>Restores a complete experiment, rejecting malformed or unsupported data.</summary>
        /// <exception cref="FormatException">The schema, numeric values or sample order is invalid.</exception>
        public Experiment Deserialize(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            try
            {
                JObject root;
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 32, DateParseHandling = DateParseHandling.None })
                {
                    root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new FormatException("Trailing content after the JSON object.");
                }
                Fields(root, "schemaVersion", "name", "modelType", "input", "configuration", "times", "states");
                if (root["schemaVersion"].Type != JTokenType.Integer || Number(root["schemaVersion"]) != SchemaVersion)
                    throw new FormatException("Unsupported schemaVersion; expected 1.");
                if (root["name"].Type != JTokenType.String) throw new FormatException("name must be a string.");
                var c = Object(root["configuration"]);
                Fields(c, "gravity", "timeStep");
                var configuration = new SimulationConfiguration(Number(c["gravity"]), Number(c["timeStep"]));
                if (root["modelType"].Type != JTokenType.String || (string)root["modelType"] != "Oberbeck")
                    throw new FormatException("Unsupported model type.");
                var input = Object(root["input"]);
                Fields(input, "mass1", "mass2", "radius", "momentOfInertia", "initialOmega", "initialPhi", "startTime", "endTime", "maxSteps");
                double maxSteps = Number(input["maxSteps"]);
                if (input["maxSteps"].Type != JTokenType.Integer || maxSteps < 1 || maxSteps > int.MaxValue)
                    throw new FormatException("maxSteps must be a positive Int32.");
                var parameters = new OberbeckParameters(Number(input["mass1"]), Number(input["mass2"]),
                    Number(input["radius"]), Number(input["momentOfInertia"]));
                var request = new SimulationRequest(parameters, Number(input["endTime"]), configuration,
                    Number(input["initialOmega"]), Number(input["initialPhi"]), Number(input["startTime"]), (int)maxSteps);
                var times = Array(root["times"]).Select(Number).ToArray();
                var states = Array(root["states"]).Select(s => Array(s).Select(Number).ToArray()).ToArray();
                return new Experiment((string)root["name"], request, new SimulationResult(times, states));
            }
            catch (JsonException error) { throw new FormatException("Invalid experiment JSON.", error); }
            catch (ArgumentException error) { throw new FormatException("Invalid experiment parameters or states.", error); }
            catch (OverflowException error) { throw new FormatException("A number is out of range.", error); }
        }
        private static JObject Object(JToken token) => token as JObject ?? throw new FormatException("Expected an object.");
        private static JArray Array(JToken token) => token as JArray ?? throw new FormatException("Expected an array.");
        private static void Fields(JObject value, params string[] names)
        {
            if (value.Properties().Any(p => !names.Contains(p.Name)) || names.Any(n => value[n] == null))
                throw new FormatException("Missing or unknown fields. Expected: " + string.Join(", ", names));
        }
        private static double Number(JToken token)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
                throw new FormatException("Expected a JSON number.");
            double value = (double)token;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new FormatException("Numbers must be finite.");
            return value;
        }
    }
}
