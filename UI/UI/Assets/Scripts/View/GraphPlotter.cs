using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphPlotter : MonoBehaviour
{
    private RawImage _graphImage;
    private TMP_Text _titleText;

    private Texture2D _texture;
    private int _width = 800;
    private int _height = 600;

    private Color _bgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    private Color _lineColor = Color.cyan;
    private Color _axisColor = Color.gray;

    // Вызывается из UICreator после Instantiate
    public void Setup(RawImage rawImage, TMP_Text titleText, string title)
    {
        _graphImage = rawImage;
        _titleText = titleText;

        if (_titleText != null) _titleText.text = title;

        _texture = new Texture2D(_width, _height);
        if (_graphImage != null) _graphImage.texture = _texture;

        Clear();
    }

    public void Clear()
    {
        if (_texture == null) return;

        Color[] pixels = new Color[_width * _height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = _bgColor;
        _texture.SetPixels(pixels);

        DrawLine(0, 0, _width - 1, 0, _axisColor);
        DrawLine(0, 0, 0, _height - 1, _axisColor);

        _texture.Apply();
    }

    public void Plot(double[] times, double[] values)
    {
        if (_texture == null) return;
        Clear();

        if (times == null || values == null || times.Length < 2) return;

        double maxTime = times[times.Length - 1];
        double maxVal = 0.0001;
        double minVal = 0;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] > maxVal) maxVal = values[i];
            if (values[i] < minVal) minVal = values[i];
        }
        if (minVal > 0) minVal = 0;

        double valRange = maxVal - minVal;

        Vector2 prevPoint = Vector2.zero;
        for (int i = 0; i < times.Length; i++)
        {
            float normX = (float)(times[i] / maxTime);
            float normY = (float)((values[i] - minVal) / valRange);

            int px = Mathf.Clamp(Mathf.RoundToInt(normX * (_width - 1)), 1, _width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(normY * (_height - 1)), 1, _height - 1);

            if (i > 0) DrawLine((int)prevPoint.x, (int)prevPoint.y, px, py, _lineColor);

            prevPoint = new Vector2(px, py);
        }

        _texture.Apply();
    }

    private void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    int px = x0 + ox, py = y0 + oy;
                    if (px >= 0 && px < _width && py >= 0 && py < _height)
                        _texture.SetPixel(px, py, color);
                }
            }

            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}