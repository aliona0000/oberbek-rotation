// Assets/Scripts/UIBootstrap.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBootstrapClass : MonoBehaviour
{
    private ISimulationBridge _bridge;
    private readonly Dictionary<string, InputField> _inputFields = new Dictionary<string, InputField>();
    private readonly Dictionary<string, Text> _outputTexts = new Dictionary<string, Text>();

    [Header("Раскладка")]
    public float panelWidth = 320f;
    public float rowHeight = 32f;
    public float padding = 8f;

    private void Start()
    {
        _bridge = new StubSimulationBridge(); // потом заменишь на реальный мост
        CreateCanvasAndUI();
    }

    private void CreateCanvasAndUI()
    {
        // --- Canvas ---
        var canvasGO = new GameObject("UI Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Панель справа ---
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.5f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, 0);
        panelRect.anchoredPosition = Vector2.zero;

        float y = -padding;

        // --- Заголовок: входные параметры ---
        y = AddHeader(panelGO.transform, "Параметры", y);

        // --- Поля ввода ---
        foreach (var p in _bridge.GetInputParameters())
        {
            y = AddInputRow(panelGO.transform, p, y);
        }

        y -= padding;

        // --- Заголовок: выходные значения ---
        y = AddHeader(panelGO.transform, "Результаты", y);

        // --- Текстовые метки ---
        foreach (var o in _bridge.GetOutputValues())
        {
            y = AddOutputRow(panelGO.transform, o, y);
        }

        y -= padding;

        // --- Кнопка «Применить» ---
        AddApplyButton(panelGO.transform, y);

        // --- Применяем дефолты сразу ---
        ApplyInputs();
    }

    private float AddHeader(Transform parent, string text, float y)
    {
        var go = new GameObject("Header_" + text);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 16;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(0, rowHeight);
        rect.anchoredPosition = new Vector2(padding, y);

        return y - rowHeight;
    }

    private float AddInputRow(Transform parent, ParameterDescriptor p, float y)
    {
        // Label
        var labelGO = new GameObject("Label_" + p.Id);
        labelGO.transform.SetParent(parent, false);
        var label = labelGO.AddComponent<Text>();
        label.text = $"{p.DisplayName} ({p.Unit})";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 13;
        label.color = Color.white;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.sizeDelta = new Vector2(-padding * 2, rowHeight);
        labelRect.anchoredPosition = new Vector2(padding, y - 16);

        // InputField
        var inputGO = new GameObject("Input_" + p.Id);
        inputGO.transform.SetParent(parent, false);
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.color = new Color(1, 1, 1, 0.9f);
        var input = inputGO.AddComponent<InputField>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6, 0);
        textRect.offsetMax = new Vector2(-6, 0);

        input.textComponent = text;
        input.text = p.DefaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 1);
        inputRect.anchorMax = new Vector2(1, 1);
        inputRect.pivot = new Vector2(0, 1);
        inputRect.sizeDelta = new Vector2(-padding * 2, rowHeight);
        inputRect.anchoredPosition = new Vector2(padding, y - 16 - 18);

        _inputFields[p.Id] = input;
        return y - (rowHeight + 22);
    }

    private float AddOutputRow(Transform parent, OutputDescriptor o, float y)
    {
        var go = new GameObject("Output_" + o.Id);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = $"{o.DisplayName}: 0 {o.Unit}";
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 13;
        t.color = Color.white;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(-padding * 2, rowHeight);
        rect.anchoredPosition = new Vector2(padding, y);

        _outputTexts[o.Id] = t;
        return y - rowHeight;
    }

    private void AddApplyButton(Transform parent, float y)
    {
        var go = new GameObject("ApplyButton");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.9f);
        var btn = go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = "Применить";
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 14;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        btn.onClick.AddListener(ApplyInputs);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(-padding * 2, rowHeight);
        rect.anchoredPosition = new Vector2(padding, y);
    }

    private void ApplyInputs()
    {
        foreach (var kv in _inputFields)
        {
            if (double.TryParse(kv.Value.text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                _bridge.SetParameter(kv.Key, value);
            }
        }
    }

    private void Update()
    {
        _bridge.Step(Time.deltaTime);

        foreach (var kv in _outputTexts)
        {
            double value = _bridge.GetOutput(kv.Key);
            kv.Value.text = $"{kv.Key}: {value:F3}";
        }
    }
}