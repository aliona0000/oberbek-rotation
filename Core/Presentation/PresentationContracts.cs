using System.Collections.Generic;

// Контракты "модель -> внешний мир". Здесь нет ничего про Unity и ничего про конкретную физику:
// модель описывает свои параметры, свои выходные величины и свою 2D-сцену из простых фигур,
// а потребитель (ViewModel/UI) только читает это описание и кадры.
// Конвенции: ось X вправо, ось Y вверх, угол поворота в градусах против часовой стрелки.
namespace OberbekRotation.Core.Presentation
{
    /// <summary>Цвет с компонентами RGBA в диапазоне от 0 до 1.</summary>
    public readonly struct Rgba
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public Rgba(float r, float g, float b, float a)
        {
            R = r; G = g; B = b; A = a;
        }

        /// <summary>Красная компонента цвета.</summary>
        public float R { get; }
        /// <summary>Зелёная компонента цвета.</summary>
        public float G { get; }
        /// <summary>Синяя компонента цвета.</summary>
        public float B { get; }
        /// <summary>Непрозрачность цвета.</summary>
        public float A { get; }
    }

    /// <summary>Вид примитива для отображения сцены.</summary>
    public enum ShapeKind
    {
        /// <summary>Прямоугольник Width x Height.</summary>
        Rectangle,
        /// <summary>Круг диаметром Width (Height игнорируется).</summary>
        Circle
    }

    /// <summary>Положение фигуры в мировых координатах сцены (центр фигуры).</summary>
    public readonly struct ElementPose
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public ElementPose(double x, double y, double rotationDegrees, double width, double height, bool visible)
        {
            X = x; Y = y; RotationDegrees = rotationDegrees; Width = width; Height = height; Visible = visible;
        }

        /// <summary>Координата центра по оси X в условных единицах сцены.</summary>
        public double X { get; }
        /// <summary>Координата центра по оси Y в условных единицах сцены.</summary>
        public double Y { get; }
        /// <summary>Угол в градусах против часовой стрелки.</summary>
        public double RotationDegrees { get; }
        /// <summary>Ширина в условных единицах сцены.</summary>
        public double Width { get; }
        /// <summary>Высота в условных единицах сцены.</summary>
        public double Height { get; }
        /// <summary>Нужно ли отображать фигуру.</summary>
        public bool Visible { get; }
    }

    /// <summary>Фигура сцены. Постоянные свойства; меняющееся положение приходит в кадрах.</summary>
    public sealed class SceneElement
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public SceneElement(string id, ShapeKind shape, Rgba color, int layer)
        {
            Id = id; Shape = shape; Color = color; Layer = layer;
        }

        /// <summary>Уникальный идентификатор элемента в соответствующей коллекции.</summary>
        public string Id { get; }
        /// <summary>Вид геометрического примитива.</summary>
        public ShapeKind Shape { get; }
        /// <summary>Цвет фигуры.</summary>
        public Rgba Color { get; }
        /// <summary>Порядок отрисовки: больше - ближе к зрителю.</summary>
        public int Layer { get; }
    }

    /// <summary>Набор фигур + границы области, которую нужно показать.</summary>
    public sealed class SceneDescription
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public SceneDescription(IReadOnlyList<SceneElement> elements,
            double minX, double maxX, double minY, double maxY, Rgba background)
        {
            Elements = elements;
            MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY;
            Background = background;
        }

        /// <summary>Фигуры в порядке, соответствующем массиву положений кадра.</summary>
        public IReadOnlyList<SceneElement> Elements { get; }
        /// <summary>Левая граница сцены.</summary>
        public double MinX { get; }
        /// <summary>Правая граница сцены.</summary>
        public double MaxX { get; }
        /// <summary>Нижняя граница сцены.</summary>
        public double MinY { get; }
        /// <summary>Верхняя граница сцены.</summary>
        public double MaxY { get; }
        /// <summary>Цвет фона сцены.</summary>
        public Rgba Background { get; }
    }

    /// <summary>Описание входного параметра для формы ввода.</summary>
    public sealed class ParameterInfo
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public ParameterInfo(string id, string displayName, string unit,
            double defaultValue, double minValue, double maxValue)
        {
            Id = id; DisplayName = displayName; Unit = unit;
            DefaultValue = defaultValue; MinValue = minValue; MaxValue = maxValue;
        }

        /// <summary>Уникальный идентификатор элемента в соответствующей коллекции.</summary>
        public string Id { get; }
        /// <summary>Название для отображения пользователю.</summary>
        public string DisplayName { get; }
        /// <summary>Единица измерения.</summary>
        public string Unit { get; }
        /// <summary>Значение при отсутствии параметра во входном словаре.</summary>
        public double DefaultValue { get; }
        /// <summary>Минимальное допустимое значение включительно.</summary>
        public double MinValue { get; }
        /// <summary>Максимальное допустимое значение включительно.</summary>
        public double MaxValue { get; }
    }

    /// <summary>Описание выходной величины (одна строка таблицы результатов).</summary>
    public sealed class OutputInfo
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public OutputInfo(string id, string displayName, string unit)
        {
            Id = id; DisplayName = displayName; Unit = unit;
        }

        /// <summary>Уникальный идентификатор элемента в соответствующей коллекции.</summary>
        public string Id { get; }
        /// <summary>Название для отображения пользователю.</summary>
        public string DisplayName { get; }
        /// <summary>Единица измерения.</summary>
        public string Unit { get; }
    }

    /// <summary>
    /// Кадр: положения всех фигур (в порядке SceneDescription.Elements)
    /// и значения всех выходных величин (в порядке ISimulationSession.Outputs).
    /// </summary>
    public sealed class SimulationFrame
    {
        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public SimulationFrame(double time, ElementPose[] poses, double[] values)
        {
            Time = time; Poses = poses; Values = values;
        }

        /// <summary>Время расчёта в секундах.</summary>
        public double Time { get; }
        /// <summary>Положения фигур в порядке описания сцены.</summary>
        public ElementPose[] Poses { get; }
        /// <summary>Выходные значения в порядке описания величин.</summary>
        public double[] Values { get; }
    }

    /// <summary>Модель целиком: что можно задать, что получится, и как запустить.</summary>
    public interface IModelDescriptor
    {
        /// <summary>Название физической модели.</summary>
        string Name { get; }
        /// <summary>Описания входных параметров.</summary>
        IReadOnlyList<ParameterInfo> Parameters { get; }
        /// <summary>Описания выходных величин в порядке значений кадра.</summary>
        IReadOnlyList<OutputInfo> Outputs { get; }

        /// <summary>
        /// Считает модель. Отсутствующие в словаре параметры берутся по умолчанию.
        /// Бросает ArgumentException с понятным сообщением при недопустимых значениях.
        /// </summary>
        ISimulationSession Start(IReadOnlyDictionary<string, double> values);
    }

    /// <summary>Один посчитанный прогон, из которого можно получать кадры в любой момент времени.</summary>
    public interface ISimulationSession
    {
        /// <summary>Описание фигур и границ сцены.</summary>
        SceneDescription Scene { get; }
        /// <summary>Описания выходных величин в порядке значений кадра.</summary>
        IReadOnlyList<OutputInfo> Outputs { get; }

        /// <summary>Сколько секунд длится показ (модель сама решает, когда остановиться).</summary>
        double Duration { get; }

        /// <summary>Сообщение, которое показывают, когда показ закончился.</summary>
        string FinishMessage { get; }

        /// <summary>Возвращает кадр для заданного времени, обрезая его до интервала показа.</summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Время равно NaN или бесконечности.</exception>
        /// <param name="time">секунды от начала показа, от 0 до Duration (за пределами - обрезается)</param>
        SimulationFrame GetFrame(double time);
    }
}
