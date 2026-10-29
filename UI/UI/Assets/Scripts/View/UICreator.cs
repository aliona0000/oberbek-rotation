using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.View
{
    /// <summary>
    /// Строит форму ввода и таблицу результатов по описаниям от ViewModel,
    /// запускает показ и обновляет значения. Не знает, какая модель за этим стоит.
    /// </summary>
    internal class UICreator : MonoBehaviour
    {
        public Transform contentArea;
        public GameObject input1;
        public GameObject output1;
        public ViewModel viewModel;  

        [Header("Необязательно: создаются автоматически, если не заданы")]
        public SceneView sceneView;
        public FramePlayer player;
        public TMP_Text statusText;   // строка для сообщений об ошибках / завершении
        public TMP_InputField inputPath;

        [Header("Проверка ввода")]
        [Tooltip("Необязательно: кнопка 'Запустить' блокируется, пока в полях есть ошибки")]
        public Button runButton;
        [Tooltip("Необязательно: кнопка 'Сброс' блокируется, пока в полях есть ошибки")]
        public Button resetButton;
        [Tooltip("Цвет подсветки поля с ошибкой")]
        public Color invalidColor = new Color(1f, 0.65f, 0.65f, 1f);

        private readonly Dictionary<string, TMP_InputField> _inputs = new Dictionary<string, TMP_InputField>();
        private readonly Dictionary<string, Color> _normalColors = new Dictionary<string, Color>();
        private readonly List<TMP_Text> _outputTexts = new List<TMP_Text>();   // порядок = FrameData.Values

        private InputValidator _validator;

        void Start()
        {
            List<global::ParameterDescriptor> parameters = viewModel.GetDefaultParameters();
            _validator = new InputValidator(parameters);

            BuildInputs(viewModel.GetDefaultParameters());
            BuildOutputs(viewModel.GetOutputs());
            EnsureViews();

            // показать установку в начальном положении (без запуска)
            Dictionary<string, double> values;
            if (CheckInputs(out values))
                Prepare(false, values);
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.FrameShown -= OnFrame;
                player.Finished -= SetStatus;
            }
        }

        void BuildInputs(List<global::ParameterDescriptor> parameters)
        {
            foreach (var p in parameters)
            {
                GameObject row = Instantiate(input1, contentArea);

                TMP_InputField input = row.GetComponentInChildren<TMP_InputField>();
                input.contentType = TMP_InputField.ContentType.DecimalNumber;
                input.text = p.DefaultValue.ToString(CultureInfo.InvariantCulture);

                // Подпись - только объект "ParamName" (иначе перезапишется и текст самого поля ввода).
                TMP_Text label = FindText(row.transform, "ParamName", 0);
                if (label != null)
                    label.text = p.DisplayName + ", " + p.Unit;

                string key = InputValidator.KeyOf(p);
                _inputs[key] = input;
                if (input.targetGraphic != null)
                    _normalColors[key] = input.targetGraphic.color;

                // проверка при каждом изменении текста (подписываемся после установки значения по умолчанию)
                input.onValueChanged.AddListener(OnInputChanged);
            }
        }

        void BuildOutputs(List<OutputData> outputs)
        {
            foreach (var o in outputs)
            {
                GameObject row = Instantiate(output1, contentArea);

                TMP_Text nameText = FindText(row.transform, "ParamName", 0);
                TMP_Text valueText = FindText(row.transform, "ParamValue", 1);

                if (nameText != null) nameText.text = o.DisplayName + ", " + o.Unit;
                if (valueText != null) valueText.text = "-";

                _outputTexts.Add(valueText);
            }
        }

        // ищем текст по имени дочернего объекта; если имя не совпало - берём по порядку в иерархии
        private static TMP_Text FindText(Transform root, string childName, int fallbackIndex)
        {
            Transform t = root.Find(childName);
            if (t != null)
            {
                var tx = t.GetComponent<TMP_Text>();
                if (tx != null) return tx;
            }
            TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
            return fallbackIndex < all.Length ? all[fallbackIndex] : null;
        }

        private void EnsureViews()
        {
            if (sceneView == null)
            {
                var go = new GameObject("SceneView");
                sceneView = go.AddComponent<SceneView>();
            }
            if (player == null)
            {
                player = sceneView.GetComponent<FramePlayer>();
                if (player == null) player = sceneView.gameObject.AddComponent<FramePlayer>();
            }
            player.view = sceneView;
            player.FrameShown += OnFrame;
            player.Finished += SetStatus;
        }

        /// <summary>на кнопку "Запустить": читает поля, просит VM посчитать и запускает показ.</summary>
        public void OnRun()
        {
            Prepare(true);
        }

        /// <summary>Необязательно: кнопка "Пауза".</summary>
        public void OnPause()
        {
            if (player != null) player.Pause();
        }

        /// <summary>Необязательно: кнопка "Сброс" - вернуть в начальное положение.</summary>
        public void OnReset()
        {
            Dictionary<string, double> values;
            if (!CheckInputs(out values)) return;
            Prepare(false, values);
        }

        public void OnExport()
        {
            string path = inputPath.text;
            viewModel.SaveExperiment(path);
        }

        private void Prepare(bool play)
        {
            SceneData scene;
            double duration;
            string error;
            if (!viewModel.TryRun(ReadInputs(), out scene, out duration, out error))
            {
                SetStatus("Error: " + error);
                return;
            }

            SetStatus("");
            sceneView.Build(scene);
            player.Load(duration, viewModel.GetFrame, viewModel.FinishMessage);
            if (play) player.Play();
        }

        private void Prepare(bool play, Dictionary<string, double> values)
        {
            SceneData scene;
            double duration;
            string error;
            if (!viewModel.TryRun(values, out scene, out duration, out error))
            {
                SetStatus("Error: " + error);
                return;
            }

            SetStatus("");
            sceneView.Build(scene);
            player.Load(duration, viewModel.GetFrame, viewModel.FinishMessage);
            if (play) player.Play();
        }
        private void OnInputChanged(string _)
        {
            Dictionary<string, double> values;
            CheckInputs(out values);
        }

        /// <summary>
        /// Двухступенчатая проверка. 1) формат и диапазоны по описаниям параметров (InputValidator);
        /// 2) правила модели (ViewModel.Validate) - только если шаг 1 пройден.
        /// Подсвечивает неверные поля, выводит сообщения и блокирует кнопки.
        /// </summary>
        private bool CheckInputs(out Dictionary<string, double> values)
        {
            List<IssueData> issues;
            _validator.Validate(GetText, out values, out issues);

            if (issues.Count == 0)
                issues = viewModel.Validate(values);

            ShowIssues(issues);
            return issues.Count == 0;
        }

        private string GetText(string key)
        {
            TMP_InputField input;
            return _inputs.TryGetValue(key, out input) ? input.text : null;
        }

        private void ShowIssues(List<IssueData> issues)
        {
            var bad = new HashSet<string>();
            var lines = new List<string>();
            foreach (var i in issues)
            {
                if (!string.IsNullOrEmpty(i.ParameterId)) bad.Add(i.ParameterId);
                lines.Add(i.Message);
            }

            foreach (var kv in _inputs)
            {
                Graphic g = kv.Value.targetGraphic;
                if (g == null) continue;
                Color normal;
                if (!_normalColors.TryGetValue(kv.Key, out normal)) normal = Color.white;
                g.color = bad.Contains(kv.Key) ? invalidColor : normal;
            }

            bool ok = issues.Count == 0;
            if (runButton != null) runButton.interactable = ok;
            if (resetButton != null) resetButton.interactable = ok;

            SetStatus(ok ? "" : string.Join("\n", lines));
        }

        private Dictionary<string, double> ReadInputs()
        {
            var values = new Dictionary<string, double>();
            foreach (var kv in _inputs)
            {
                // допускаем и запятую, и точку
                string s = kv.Value.text.Trim().Replace(',', '.');
                double v;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    values[kv.Key] = v;
            }
            return values;
        }

        private void OnFrame(FrameData frame)
        {
            int count = Mathf.Min(_outputTexts.Count, frame.Values.Length);
            for (int i = 0; i < count; i++)
            {
                if (_outputTexts[i] != null)
                    _outputTexts[i].text = frame.Values[i].ToString("F3", CultureInfo.InvariantCulture);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}