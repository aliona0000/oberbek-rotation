using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class OberbeckScene1 : MonoBehaviour
{
    [Header("Параметры установки")]
    public float discMass = 2.0f;
    public float discRadius = 1.2f;
    public float pulleyRadius = 0.15f;
    public float weightMass = 0.5f;
    public float gravity = 9.81f;

    [Header("Начальные условия")]
    public float initialOmega = 0f;
    public float initialPhi = 0f;

    [Header("Визуал")]
    public Color backgroundColor = new Color(0.95f, 0.95f, 0.95f);
    public Color discColor = new Color(0.3f, 0.5f, 0.8f);
    public Color weightColor = new Color(0.6f, 0.3f, 0.1f);
    public Color threadColor = Color.black;

    [Header("Нить")]
    [Tooltip("Максимальная длина нити, м. Груз опускается на эту длину и останавливается.")]
    public float threadLength = 2.0f;

    // ---- Состояние ----
    private double _omega;
    private double _phi;
    private double _time;
    private float _threadUnwound;   // сколько нити уже смотано, м

    private Transform _discPivot;
    private Transform _disc;
    private Transform _weight;
    private LineRenderer _thread;

    // ---- UI ----
    private readonly Dictionary<string, InputField> _inputs = new Dictionary<string, InputField>();
    private readonly Dictionary<string, Text> _outputs = new Dictionary<string, Text>();

    private void Start()
    {
        BuildScene();
        BuildUI();
        ApplyParameters();
        ResetSimulation();
    }

    // ============================================================
    //  СЦЕНА
    // ============================================================
    private void BuildScene()
    {
        // Камера
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 3.5f;
        cam.backgroundColor = backgroundColor;
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0, 0, -10);

        // Свет
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ---- Диск ----
        // Pivot в центре, крутится вокруг Z.
        var pivotGO = new GameObject("DiscPivot");
        pivotGO.transform.position = Vector3.zero;
        _discPivot = pivotGO.transform;

        // Сам цилиндр — ребёнок pivot, повёрнут один раз, больше не трогаем.
        var discGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discGO.name = "Disc";
        discGO.transform.SetParent(_discPivot, false);
        discGO.transform.localPosition = Vector3.zero;
        discGO.transform.localRotation = Quaternion.Euler(90, 0, 0); // лежит как колесо
        discGO.transform.localScale = new Vector3(discRadius * 2, 0.1f, discRadius * 2);
        discGO.GetComponent<Renderer>().material.color = discColor;
        _disc = discGO.transform;

        // Шкив — маленький цилиндр в центре, тоже ребёнок pivot
        var pulleyGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pulleyGO.name = "Pulley";
        pulleyGO.transform.SetParent(_discPivot, false);
        pulleyGO.transform.localPosition = Vector3.zero;
        pulleyGO.transform.localRotation = Quaternion.Euler(90, 0, 0);
        pulleyGO.transform.localScale = new Vector3(pulleyRadius * 2, 0.2f, pulleyRadius * 2);
        pulleyGO.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);

        // ---- Нить ----
        var threadGO = new GameObject("Thread");
        _thread = threadGO.AddComponent<LineRenderer>();
        _thread.positionCount = 2;
        _thread.startWidth = 0.02f;
        _thread.endWidth = 0.02f;
        _thread.material = new Material(Shader.Find("Sprites/Default"));
        _thread.startColor = threadColor;
        _thread.endColor = threadColor;
        _thread.useWorldSpace = true;

        // ---- Груз ----
        var weightGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weightGO.name = "Weight";
        weightGO.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        weightGO.GetComponent<Renderer>().material.color = weightColor;
        _weight = weightGO.transform;
    }

    /// <summary>
    /// Точка на шкиве, откуда сходит нить (в мировых координатах).
    /// Лежит справа от центра на расстоянии pulleyRadius.
    /// </summary>
    private Vector3 GetThreadAttachPoint()
    {
        // Точка крепления — на краю шкива. Она не зависит от угла диска,
        // потому что мы считаем, что нить всегда сходит в одной точке (справа).
        // Если хочешь, чтобы точка ехала вместе с диском — раскомментируй нижнюю строку.
        //return _discPivot.TransformPoint(new Vector3(pulleyRadius, 0, 0));

        return new Vector3(pulleyRadius, 0f, 0f);
    }

    private void UpdateThreadVisual()
    {
        if (_thread == null || _weight == null) return;

        Vector3 attach = GetThreadAttachPoint();
        Vector3 weightPos = _weight.position;
        _thread.SetPosition(0, attach);
        _thread.SetPosition(1, weightPos);
    }

    // ============================================================
    //  UI
    // ============================================================
    private void BuildUI()
    {
        var canvasGO = new GameObject("UI Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 0.5f);
        panelRect.sizeDelta = new Vector2(320, 0);
        panelRect.anchoredPosition = Vector2.zero;

        float y = -10f;
        float rowH = 30f;
        float pad = 10f;

        y = AddHeader(panelGO.transform, "Параметры", y, rowH, pad);
        y = AddInputRow(panelGO.transform, "discMass", "Масса диска, кг", discMass.ToString(CultureInfo.InvariantCulture), y, rowH, pad);
        y = AddInputRow(panelGO.transform, "discRadius", "Радиус диска, м", discRadius.ToString(CultureInfo.InvariantCulture), y, rowH, pad);
        y = AddInputRow(panelGO.transform, "pulleyRadius", "Радиус шкива, м", pulleyRadius.ToString(CultureInfo.InvariantCulture), y, rowH, pad);
        y = AddInputRow(panelGO.transform, "weightMass", "Масса груза, кг", weightMass.ToString(CultureInfo.InvariantCulture), y, rowH, pad);
        y = AddInputRow(panelGO.transform, "gravity", "Ускорение g, м/с²", gravity.ToString(CultureInfo.InvariantCulture), y, rowH, pad);
        y = AddInputRow(panelGO.transform, "threadLength", "Длина нити, м", threadLength.ToString(CultureInfo.InvariantCulture), y, rowH, pad);

        y -= pad;
        y = AddHeader(panelGO.transform, "Результаты", y, rowH, pad);
        y = AddOutputRow(panelGO.transform, "time", "Время", "с", y, rowH, pad);
        y = AddOutputRow(panelGO.transform, "omega", "Угловая скорость", "рад/с", y, rowH, pad);
        y = AddOutputRow(panelGO.transform, "phi", "Угол", "рад", y, rowH, pad);
        y = AddOutputRow(panelGO.transform, "alpha", "Угловое ускорение", "рад/с²", y, rowH, pad);

        y -= pad;
        AddButton(panelGO.transform, "Применить", ApplyParameters, y, rowH, pad);
        y -= rowH + pad;
        AddButton(panelGO.transform, "Сбросить", ResetSimulation, y, rowH, pad);
    }

    private float AddHeader(Transform parent, string text, float y, float rowH, float pad)
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
        rect.sizeDelta = new Vector2(-pad * 2, rowH);
        rect.anchoredPosition = new Vector2(pad, y);
        return y - rowH - 4;
    }

    private float AddInputRow(Transform parent, string id, string label, string defaultValue, float y, float rowH, float pad)
    {
        var labelGO = new GameObject("Label_" + id);
        labelGO.transform.SetParent(parent, false);
        var lbl = labelGO.AddComponent<Text>();
        lbl.text = label;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 13;
        lbl.color = Color.white;
        var lrect = labelGO.GetComponent<RectTransform>();
        lrect.anchorMin = new Vector2(0, 1);
        lrect.anchorMax = new Vector2(1, 1);
        lrect.pivot = new Vector2(0, 1);
        lrect.sizeDelta = new Vector2(-pad * 2, 18);
        lrect.anchoredPosition = new Vector2(pad, y);

        var inputGO = new GameObject("Input_" + id);
        inputGO.transform.SetParent(parent, false);
        var img = inputGO.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.9f);
        var input = inputGO.AddComponent<InputField>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 14;
        txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleLeft;
        var trect = textGO.GetComponent<RectTransform>();
        trect.anchorMin = Vector2.zero;
        trect.anchorMax = Vector2.one;
        trect.offsetMin = new Vector2(6, 0);
        trect.offsetMax = new Vector2(-6, 0);

        input.textComponent = txt;
        input.text = defaultValue;

        var irect = inputGO.GetComponent<RectTransform>();
        irect.anchorMin = new Vector2(0, 1);
        irect.anchorMax = new Vector2(1, 1);
        irect.pivot = new Vector2(0, 1);
        irect.sizeDelta = new Vector2(-pad * 2, 24);
        irect.anchoredPosition = new Vector2(pad, y - 20);

        _inputs[id] = input;
        return y - rowH - 20;
    }

    private float AddOutputRow(Transform parent, string id, string label, string unit, float y, float rowH, float pad)
    {
        var go = new GameObject("Output_" + id);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = $"{label}: 0 {unit}";
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 13;
        t.color = Color.white;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(-pad * 2, rowH);
        rect.anchoredPosition = new Vector2(pad, y);
        _outputs[id] = t;
        return y - rowH;
    }

    private void AddButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, float y, float rowH, float pad)
    {
        var go = new GameObject("Button_" + text);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.9f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 14;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var trect = textGO.GetComponent<RectTransform>();
        trect.anchorMin = Vector2.zero;
        trect.anchorMax = Vector2.one;
        trect.offsetMin = Vector2.zero;
        trect.offsetMax = Vector2.zero;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(-pad * 2, rowH);
        rect.anchoredPosition = new Vector2(pad, y);
    }

    // ============================================================
    //  ЛОГИКА
    // ============================================================
    private void ApplyParameters()
    {
        discMass = ParseInput("discMass", discMass);
        discRadius = ParseInput("discRadius", discRadius);
        pulleyRadius = ParseInput("pulleyRadius", pulleyRadius);
        weightMass = ParseInput("weightMass", weightMass);
        gravity = ParseInput("gravity", gravity);
        threadLength = ParseInput("threadLength", threadLength);

        if (_disc != null)
        {
            _disc.localScale = new Vector3(discRadius * 2, 0.1f, discRadius * 2);
        }
    }

    private float ParseInput(string id, float fallback)
    {
        if (_inputs.TryGetValue(id, out var input) &&
            float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        return fallback;
    }

    private void ResetSimulation()
    {
        _omega = initialOmega;
        _phi = initialPhi;
        _time = 0;
        _threadUnwound = 0f;

        if (_discPivot != null) _discPivot.localRotation = Quaternion.identity;

        if (_weight != null)
        {
            Vector3 attach = GetThreadAttachPoint();
            _weight.position = new Vector3(attach.x, attach.y, 0);
        }

        UpdateThreadVisual();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // --- Физика ---
        double I_disc = 0.5 * discMass * discRadius * discRadius;
        double I_total = I_disc + weightMass * pulleyRadius * pulleyRadius;
        double alpha = (weightMass * gravity * pulleyRadius) / I_total;

        // Останавливаем вращение, когда нить размоталась до конца.
        bool threadEnded = _threadUnwound >= threadLength;

        if (!threadEnded)
        {
            _omega += alpha * dt;
            _phi += _omega * dt;
            _threadUnwound += (float)(_omega * pulleyRadius * dt);
        }

        _time += dt;

        // --- Визуал диска: только поворот вокруг Z ---
        if (_discPivot != null)
        {
            _discPivot.localRotation = Quaternion.Euler(0, 0, -((float)_phi) * Mathf.Rad2Deg);
        }

        // --- Визуал груза: опускается ровно на длину смотанной нити ---
        if (_weight != null)
        {
            Vector3 attach = GetThreadAttachPoint();
            float weightY = attach.y - _threadUnwound;
            _weight.position = new Vector3(attach.x, weightY, 0);
        }

        UpdateThreadVisual();

        // --- UI ---
        UpdateOutput("time", _time);
        UpdateOutput("omega", _omega);
        UpdateOutput("phi", _phi);
        UpdateOutput("alpha", threadEnded ? 0 : alpha);
    }

    private void UpdateOutput(string id, double value)
    {
        if (!_outputs.TryGetValue(id, out var text)) return;

        string unit = id switch
        {
            "time" => "с",
            "omega" => "рад/с",
            "phi" => "рад",
            "alpha" => "рад/с²",
            _ => ""
        };

        string label = id switch
        {
            "time" => "Время",
            "omega" => "Угловая скорость",
            "phi" => "Угол",
            "alpha" => "Угловое ускорение",
            _ => id
        };

        text.text = $"{label}: {value:F3} {unit}";
    }
}