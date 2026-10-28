using System.Collections.Generic;
using UnityEngine;

// Данные, которыми ViewModel общается с View.
// Это "нейтральные" типы: в них нет ни Core, ни какой-либо физики - только "что и где нарисовать".
// Параметры ввода описываются существующим ParameterDescriptor.

public enum ShapeType
{
    Rectangle,   // Width x Height
    Circle       // диаметр = Width
}

/// <summary>Одна фигура сцены (постоянные свойства).</summary>
public sealed class ElementData
{
    public string Id;
    public ShapeType Shape;
    public Color Color;
    public int Layer;   // больше - ближе к зрителю
}

/// <summary>Что нужно нарисовать: набор фигур, границы видимой области, фон.</summary>
public sealed class SceneData
{
    public List<ElementData> Elements = new List<ElementData>();
    public Rect Bounds;           // x,y - левый нижний угол
    public Color Background;
}

/// <summary>Положение фигуры: центр, поворот (градусы, против часовой), размеры.</summary>
public struct PoseData
{
    public float X, Y, Rotation, Width, Height;
    public bool Visible;
}

/// <summary>Один кадр: позы всех фигур (в порядке SceneData.Elements) и значения величин (в порядке GetOutputs()).</summary>
public sealed class FrameData
{
    public double Time;
    public PoseData[] Poses;
    public double[] Values;
}

/// <summary>Описание одной выводимой величины.</summary>
public sealed class OutputData
{
    public string Id;
    public string DisplayName;
    public string Unit;
}

/// <summary>Замечание к введённому значению. ParameterId - к какому полю относится (может быть пустым).</summary>
public sealed class IssueData
{
    public string ParameterId;
    public string Message;
}