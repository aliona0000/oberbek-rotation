using System.Collections.Generic;
using System.Globalization;

namespace Assets.Scripts.View
{
    /// <summary>
    /// Проверка текста в полях ввода по описаниям параметров (ParameterDescriptor):
    /// поле не пустое, это число (точка или запятая), конечное, лежит в [MinValue, MaxValue].
    /// Чистая логика без Unity-объектов, поэтому её легко тестировать. Физики не знает:
    /// правила модели (связи между параметрами) проверяет ViewModel.Validate.
    /// </summary>
    public sealed class InputValidator
    {
        private readonly List<global::ParameterDescriptor> _parameters;

        public InputValidator(IEnumerable<global::ParameterDescriptor> parameters)
        {
            _parameters = new List<global::ParameterDescriptor>(parameters);
        }

        /// <summary>Ключ поля: Id, а если его нет - название.</summary>
        public static string KeyOf(global::ParameterDescriptor p)
        {
            return string.IsNullOrEmpty(p.Id) ? p.DisplayName : p.Id;
        }

        /// <summary>Читает число из текста; допускает запятую вместо точки и пробелы по краям.</summary>
        public static bool TryParse(string text, out double value)
        {
            value = 0.0;
            if (text == null) return false;
            string s = text.Trim().Replace(',', '.');
            if (s.Length == 0) return false;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                   && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>
        /// Проверяет все поля. Корректные значения попадают в values, замечания - в issues.
        /// </summary>
        /// <param name="getText">Функция "ключ поля -> введённый текст".</param>
        /// <returns>true, если замечаний нет.</returns>
        public bool Validate(System.Func<string, string> getText,
            out Dictionary<string, double> values, out List<IssueData> issues)
        {
            values = new Dictionary<string, double>();
            issues = new List<IssueData>();

            foreach (var p in _parameters)
            {
                string key = KeyOf(p);
                string text = getText(key);
                string name = p.DisplayName;

                if (string.IsNullOrWhiteSpace(text))
                {
                    issues.Add(Issue(key, name + " is required."));
                    continue;
                }

                double v;
                if (!TryParse(text, out v))
                {
                    issues.Add(Issue(key, name + " must be a number."));
                    continue;
                }

                if (v < p.MinValue || v > p.MaxValue)
                {
                    issues.Add(Issue(key, string.Format(CultureInfo.InvariantCulture,
                        "{0} must be between {1} and {2} {3}.", name, Format(p.MinValue), Format(p.MaxValue), p.Unit)));
                    continue;
                }

                values[key] = v;
            }

            return issues.Count == 0;
        }

        private static IssueData Issue(string key, string message)
        {
            return new IssueData { ParameterId = key, Message = message };
        }

        private static string Format(double x)
        {
            return x.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
