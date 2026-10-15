using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.View
{
    /// <summary>
    /// Универсальный 2D-рендер: строит фигуры по SceneData и расставляет их по FrameData.
    /// Не знает, что именно нарисовано. Фигуры - SpriteRenderer, спрайты (квадрат и круг) создаются в коде.
    /// Камера подгоняется под SceneData.Bounds.
    /// </summary>
    public class SceneView : MonoBehaviour
    {
        [Tooltip("Подгонять ортографическую камеру под границы сцены")]
        public bool controlCamera = true;
        [Tooltip("Запас вокруг границ сцены (в единицах сцены)")]
        public float margin = 0.3f;
        [Tooltip("Сдвиг центра кадра, например чтобы сцена не пряталась под панелью ввода")]
        public Vector2 cameraOffset = Vector2.zero;

        private readonly List<Transform> _nodes = new List<Transform>();
        private readonly List<ShapeType> _shapes = new List<ShapeType>();

        private Rect _bounds;
        private bool _hasBounds;
        private int _lastScreenW, _lastScreenH;

        private static Sprite _squareSprite;
        private static Sprite _circleSprite;

        public void Build(SceneData scene)
        {
            Clear();

            foreach (var e in scene.Elements)
            {
                var go = new GameObject(e.Id);
                go.transform.SetParent(transform, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = e.Shape == ShapeType.Circle ? Circle() : Square();
                sr.color = e.Color;
                sr.sortingOrder = e.Layer;

                _nodes.Add(go.transform);
                _shapes.Add(e.Shape);
            }

            _bounds = scene.Bounds;
            _hasBounds = true;
            if (controlCamera) FitCamera(scene.Background);
        }

        public void Apply(FrameData frame)
        {
            int count = Mathf.Min(_nodes.Count, frame.Poses.Length);
            for (int i = 0; i < count; i++)
            {
                PoseData p = frame.Poses[i];
                Transform t = _nodes[i];

                if (t.gameObject.activeSelf != p.Visible) t.gameObject.SetActive(p.Visible);
                if (!p.Visible) continue;

                float height = _shapes[i] == ShapeType.Circle ? p.Width : p.Height;
                t.localPosition = new Vector3(p.X, p.Y, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, p.Rotation);
                t.localScale = new Vector3(p.Width, height, 1f);
            }
        }

        public void Clear()
        {
            foreach (var t in _nodes)
                if (t != null) Destroy(t.gameObject);
            _nodes.Clear();
            _shapes.Clear();
        }

        // при изменении размера окна перегоняем камеру под новые пропорции
        private void LateUpdate()
        {
            if (!controlCamera || !_hasBounds) return;
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
                FitCamera(null);
        }

        private void FitCamera(Color? background)
        {
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;

            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            float w = _bounds.width + margin * 2f;
            float h = _bounds.height + margin * 2f;
            float aspect = cam.aspect > 0f ? cam.aspect : 1f;

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(h * 0.5f, w * 0.5f / aspect);
            cam.transform.rotation = Quaternion.identity;
            cam.transform.position = new Vector3(_bounds.center.x + cameraOffset.x, _bounds.center.y + cameraOffset.y, -10f);

            if (background.HasValue)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background.Value;
            }
        }

        // белый квадрат 1x1 юнит
        private static Sprite Square()
        {
            if (_squareSprite == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return _squareSprite;
        }

        // белый круг диаметром 1 юнит со сглаженным краем
        private static Sprite Circle()
        {
            if (_circleSprite == null)
            {
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color[n * n];
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(n * 0.5f - d));
                    }
                }
                tex.SetPixels(px);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                _circleSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
            return _circleSprite;
        }
    }
}