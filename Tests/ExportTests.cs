using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OberbekRotation.Core;
using OberbekRotation.Export;
using Xunit;

namespace OberbekRotation.Tests
{
    public class ExportTests
    {
        private static Experiment Example()
        {
            var input = new SimulationRequest(new OberbeckParameters(0.2, 0.15, 0.05, 0.01), 2,
                new SimulationConfiguration(9.81, 0.3), initialOmega: -0.25, initialPhi: 0.75);
            return new Experiment("Обербек \"опыт\", №1\nтест", input, new OberbeckSimulation().Run(input));
        }

        [Fact]
        public void JsonRoundTripRestoresAllInputsAndSamplesAndCanRerun()
        {
            var original = Example();
            var serializer = new JsonExperimentSerializer();
            var loaded = serializer.Deserialize(serializer.Serialize(original));
            Assert.Equal(original.Name, loaded.Name);
            Assert.Equal(original.Input.Parameters.Mass1, loaded.Input.Parameters.Mass1);
            Assert.Equal(original.Input.Parameters.Mass2, loaded.Input.Parameters.Mass2);
            Assert.Equal(original.Input.Parameters.Radius, loaded.Input.Parameters.Radius);
            Assert.Equal(original.Model.MomentOfInertia, loaded.Model.MomentOfInertia);
            Assert.Equal(original.Model.Torque, loaded.Model.Torque);
            Assert.Equal(original.Configuration.Gravity, loaded.Configuration.Gravity);
            Assert.Equal(original.Configuration.TimeStep, loaded.Configuration.TimeStep);
            Assert.Equal(original.Input.MaxSteps, loaded.Input.MaxSteps);
            Assert.Equal(original.Result.Times, loaded.Result.Times);
            var rerun = new OberbeckSimulation().Run(loaded.Input).States;
            var states = loaded.Result.States;
            for (int i = 0; i < states.Length; i++)
            {
                Assert.Equal(original.Result.States[i], states[i]);
                Assert.Equal(states[i], rerun[i]);
            }
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
        [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)]
        public void InvalidJsonSchemaOrPhysicsIsRejected(int scenario)
        {
            var serializer = new JsonExperimentSerializer();
            var json = JObject.Parse(serializer.Serialize(Example()));
            switch (scenario)
            {
                case 0: json["schemaVersion"] = 99; break;
                case 1: json.Remove("configuration"); break;
                case 2: json["input"]["radius"] = 0; break;
                case 3: json["input"]["mass1"] = -1; break;
                case 4: json["input"]["momentOfInertia"] = "0.01"; break;
                case 5: json["states"][1] = new JArray(1); break;
                case 6: json["times"][1] = 0; break;
                case 7: json["modelType"] = "Other"; break;
                case 8: json["unknown"] = true; break;
                case 9: json["input"]["maxSteps"] = 2.5; break;
                case 10: json["input"]["maxSteps"] = 1; break;
                case 11: json["states"][0][0] = 99; break;
                case 12: json["configuration"]["gravity"] = "NaN"; break;
                case 13: json["times"] = new JArray(); break;
            }
            Assert.Throws<FormatException>(() => serializer.Deserialize(json.ToString()));
        }

        [Theory]
        [InlineData("")] [InlineData("null")] [InlineData("[]")] [InlineData("{")]
        [InlineData("{\"schemaVersion\":1,\"schemaVersion\":2}")]
        public void MalformedJsonIsRejected(string json)
        {
            Assert.Throws<FormatException>(() => new JsonExperimentSerializer().Deserialize(json));
        }

        [Fact]
        public void TrailingJsonAndNullArgumentsAreRejected()
        {
            var serializer = new JsonExperimentSerializer();
            Assert.Throws<FormatException>(() => serializer.Deserialize(serializer.Serialize(Example()) + " {}"));
            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(null));
            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));
        }

        [Theory]
        [InlineData("ru-RU", ',')]
        [InlineData("en-US", ';')]
        [InlineData("de-DE", ',')]
        public void CsvUsesInvariantNumbersUnitsAndCorrectEnergy(string culture, char delimiter)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var experiment = Example();
                string csv = new CsvExperimentSerializer().Serialize(experiment, delimiter);
                var lines = csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(experiment.Result.Count + 1, lines.Length);
                Assert.Equal("time_s", lines[0].Split(delimiter)[0]);
                var values = lines.Last().Split(delimiter).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                Assert.Equal(5, values.Length);
                Assert.Equal(2, values[0]);
                Assert.Equal(4.655, values[1], 10);
                Assert.Equal(5.155, values[2], 10);
                Assert.Equal(2.4525, values[3], 10);
                Assert.Equal(0.5 * 0.01 * values[1] * values[1], values[4], 10);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Fact]
        public void FilesRoundTripUtf8AndUnsupportedFormatDoesNotOverwrite()
        {
            string directory = Path.Combine(Path.GetTempPath(), "oberbek-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var experiment = Example();
                string path = Path.Combine(directory, "experiment.JSON");
                ExperimentFile.Save(path, experiment);
                Assert.Equal(experiment.Name, ExperimentFile.Load(path).Name);
                ExperimentFile.Save(Path.Combine(directory, "data.csv"), experiment, ';');
                Assert.StartsWith("time_s;", File.ReadAllText(Path.Combine(directory, "data.csv")));
                string unsupported = Path.Combine(directory, "keep.txt");
                File.WriteAllText(unsupported, "keep");
                Assert.Throws<NotSupportedException>(() => ExperimentFile.Save(unsupported, experiment));
                Assert.Equal("keep", File.ReadAllText(unsupported));
                Assert.Throws<NotSupportedException>(() => ExperimentFile.Load(Path.Combine(directory, "data.csv")));
                Assert.Throws<ArgumentOutOfRangeException>(() => new CsvExperimentSerializer().Serialize(experiment, '|'));
            }
            finally { Directory.Delete(directory, true); }
        }
    }
}
