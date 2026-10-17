using System;
using System.Globalization;
using System.Text;

namespace OberbekRotation.Export
{
    /// <summary>Exports a numeric measurement table; JSON restores the full experiment.</summary>
    public sealed class CsvExperimentSerializer
    {
        /// <summary>Serializes t, omega, phi, alpha and rotational energy with SI-unit headers.</summary>
        /// <param name="experiment">Experiment to export.</param>
        /// <param name="delimiter">Comma or semicolon; defaults to comma.</param>
        public string Serialize(Experiment experiment, char delimiter = ',')
        {
            if (experiment == null) throw new ArgumentNullException(nameof(experiment));
            if (delimiter != ',' && delimiter != ';') throw new ArgumentOutOfRangeException(nameof(delimiter));
            var result = new StringBuilder();
            result.Append(string.Join(delimiter.ToString(), new[] { "time_s", "omega_rad_s", "phi_rad",
                "alpha_rad_s2", "kinetic_energy_J" })).Append("\r\n");
            var times = experiment.Result.Times;
            var states = experiment.Result.States;
            for (int row = 0; row < times.Length; row++)
            {
                double omega = states[row][0];
                double[] values = { times[row], omega, states[row][1], experiment.Model.AngularAcceleration,
                    0.5 * experiment.Model.MomentOfInertia * omega * omega };
                for (int i = 0; i < values.Length; i++)
                {
                    if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
                        throw new ArgumentOutOfRangeException(nameof(experiment), "A derived CSV value is not finite.");
                    if (i != 0) result.Append(delimiter);
                    result.Append(values[i].ToString("R", CultureInfo.InvariantCulture));
                }
                result.Append("\r\n");
            }
            return result.ToString();
        }
    }
}
