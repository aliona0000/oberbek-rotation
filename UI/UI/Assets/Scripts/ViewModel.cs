using OberbekRotation.Core.Presentation;
using OberbekRotation.Export;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Посредник между View и Core. Физики не знает: берёт у Core описания (параметры, величины, сцена, кадры)
/// и переводит их в нейтральные типы из ViewData.cs, которые понимает View.
/// </summary>
public class ViewModel : MonoBehaviour
{
    // Выбирается конкретная модель
    private readonly IModelDescriptor _model = new OberbeckModelDescriptor();

    private ISimulationSession _session;
    private FrameData _frame;

    // Список описаний параметров, которые UI показывает и редактирует.
    public List<ParameterDescriptor> GetDefaultParameters()
    {
        var list = new List<ParameterDescriptor>();
        foreach (var p in _model.Parameters)
        {
            list.Add(new ParameterDescriptor
            {
                Id = p.Id,
                DisplayName = p.DisplayName,
                Unit = p.Unit,
                DefaultValue = p.DefaultValue,
                MinValue = p.MinValue,
                MaxValue = p.MaxValue
            });
        }
        return list;
    }

    // Список выводимых величин (порядок = порядок FrameData.Values).
    public List<OutputData> GetOutputs()
    {
        var list = new List<OutputData>();
        foreach (var o in _model.Outputs)
            list.Add(new OutputData { Id = o.Id, DisplayName = o.DisplayName, Unit = o.Unit });
        return list;
    }

    /// <summary>
    /// Проверка введённых значений по правилам модели без расчёта (диапазоны, связи между параметрами).
    /// Пустой список - можно запускать.
    /// </summary>
    public List<IssueData> Validate(Dictionary<string, double> values)
    {
        var list = new List<IssueData>();
        foreach (var i in _model.Validate(values))
            list.Add(new IssueData { ParameterId = i.ParameterId, Message = i.Message });
        return list;
    }

    /// <summary>
    /// Вызывается, когда пользователь нажал «Запустить». values - словарь id → введённое значение.
    /// При успехе отдаёт описание сцены и длительность показа в секундах; при ошибке - текст.
    /// </summary>
    public bool TryRun(Dictionary<string, double> values, out SceneData scene, out double duration, out string error)
    {
        foreach(var kv in values)
        Debug.Log($"[ViewModel] Получен параметр: {kv.Key} = {kv.Value}");

        scene = null;
        duration = 0.0;
        error = null;
        try
        {
            ISimulationSession session = _model.Start(values);

            scene = MapScene(session.Scene);
            duration = session.Duration;

            _session = session;
            _frame = new FrameData
            {
                Poses = new PoseData[scene.Elements.Count],
                Values = new double[session.Outputs.Count]
            };
            return true;
        }
        catch (Exception e)   // Core бросает ArgumentException / InvalidOperationException
        {
            error = e.Message;
            return false;
        }
    }

    /// <summary>Сообщение по окончании показа (задаёт модель).</summary>
    public string FinishMessage
    {
        get { return _session == null ? "" : _session.FinishMessage; }
    }

    /// <summary>Кадр на момент time (секунды от начала показа). Объект переиспользуется между вызовами.</summary>
    public FrameData GetFrame(double time)
    {
        if (_session == null) return null;

        SimulationFrame f = _session.GetFrame(time);
        _frame.Time = f.Time;

        for (int i = 0; i < _frame.Poses.Length; i++)
        {
            ElementPose p = f.Poses[i];
            _frame.Poses[i] = new PoseData
            {
                X = (float)p.X,
                Y = (float)p.Y,
                Rotation = (float)p.RotationDegrees,
                Width = (float)p.Width,
                Height = (float)p.Height,
                Visible = p.Visible
            };
        }
        for (int i = 0; i < _frame.Values.Length; i++)
            _frame.Values[i] = f.Values[i];

        return _frame;
    }

    // Сохранение последнего показанного прогона в файл (кнопка «Экспорт»)
    public void SaveExperiment(string outputDirectory)
    {
        var exportable = _session as IExportableSession;
        if (exportable == null)
            throw new InvalidOperationException("Nothing to export: run the simulation first.");

        Directory.CreateDirectory(outputDirectory);

        var experiment = new Experiment(exportable.ExperimentName, exportable.Request, exportable.Result);

        string jsonPath = Path.Combine(outputDirectory, "oberbeck.json");
        ExperimentFile.Save(jsonPath, experiment);
        ExperimentFile.Save(Path.Combine(outputDirectory, "oberbeck.csv"), experiment);

        Debug.Log("Saved to " + Path.GetFullPath(outputDirectory));
    }

    private static SceneData MapScene(SceneDescription s)
    {
        var scene = new SceneData
        {
            Bounds = new Rect((float)s.MinX, (float)s.MinY, (float)(s.MaxX - s.MinX), (float)(s.MaxY - s.MinY)),
            Background = ToColor(s.Background)
        };

        foreach (var e in s.Elements)
        {
            scene.Elements.Add(new ElementData
            {
                Id = e.Id,
                Shape = e.Shape == ShapeKind.Circle ? ShapeType.Circle : ShapeType.Rectangle,
                Color = ToColor(e.Color),
                Layer = e.Layer
            });
        }
        return scene;
    }

    private static Color ToColor(Rgba c)
    {
        return new Color(c.R, c.G, c.B, c.A);
    }
}