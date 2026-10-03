using UnityEngine;

public class SceneBootstrap : MonoBehaviour
{
    [Header("Настройки сцены")]
    public float discRadius = 1.5f;
    public float discThickness = 0.15f;
    public Color discColor = new Color(0.3f, 0.5f, 0.8f);
    public Color threadColor = Color.black;
    public Color weightColor = new Color(0.6f, 0.3f, 0.1f);

    [Header("Параметры по умолчанию")]
    public float initialOmega = 0f;
    public float initialPhi = 0f;

    private void Start()
    {
        CreateCamera();
        CreateLight();
        CreateDisc();
        CreateThread();
        CreateWeight();
        CreateUIRoot();
    }

    private void CreateCamera()
    {
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(7, -1, -10);
    }

    private void CreateLight()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    private void CreateDisc()
    {
        var discGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discGO.name = "Disc";
        discGO.transform.position = Vector3.zero;
        discGO.transform.localScale = new Vector3(discRadius * 2, discThickness, discRadius * 2);
        // Cylinder в Unity стоит вертикально — повернём, чтобы был как колесо
        discGO.transform.rotation = Quaternion.Euler(90, 0, 0);

        var renderer = discGO.GetComponent<Renderer>();
        renderer.material.color = discColor;

        var controller = discGO.AddComponent<SimpleRotation>();
        controller.initialOmega = initialOmega;
        controller.initialPhi = initialPhi;
    }

    private void CreateThread()
    {
        var threadGO = new GameObject("Thread");
        var line = threadGO.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.03f;
        line.endWidth = 0.03f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = threadColor;
        line.endColor = threadColor;
        // Нить от края диска вниз
        line.SetPosition(0, new Vector3(discRadius, 0, 0));
        line.SetPosition(1, new Vector3(discRadius, -2f, 0));
    }

    private void CreateWeight()
    {
        var weightGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weightGO.name = "Weight";
        weightGO.transform.position = new Vector3(discRadius, -2.2f, 0);
        weightGO.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        weightGO.GetComponent<Renderer>().material.color = weightColor;
    }

    private void CreateUIRoot()
    {
        var canvasGO = new GameObject("UI Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        // Позже добавишь сюда панели с InputField и текстом
    }
}