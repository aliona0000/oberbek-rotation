using System;
using System.IO;
using System.Text;

namespace OberbekRotation.Export
{
    /// <summary>UTF-8 file export selected by .json or .csv extension.</summary>
    public static class ExperimentFile
    {
        /// <summary>Saves an experiment as JSON or a CSV table, replacing an existing file.</summary>
        /// <remarks>The parent directory must exist. Serialization completes before the file is opened.</remarks>
        public static void Save(string path, Experiment experiment, char csvDelimiter = ',')
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            string text;
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".json": text = new JsonExperimentSerializer().Serialize(experiment); break;
                case ".csv": text = new CsvExperimentSerializer().Serialize(experiment, csvDelimiter); break;
                default: throw new NotSupportedException("Choose a .json or .csv file.");
            }
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }
        /// <summary>Loads a complete experiment from a UTF-8 .json file.</summary>
        /// <remarks>CSV contains measurements only, and cannot restore apparatus settings.</remarks>
        public static Experiment Load(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Restoring an experiment requires a .json file.");
            return new JsonExperimentSerializer().Deserialize(File.ReadAllText(path, Encoding.UTF8));
        }
    }
}
