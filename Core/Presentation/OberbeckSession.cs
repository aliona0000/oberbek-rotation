using System;
using System.Collections.Generic;

namespace OberbekRotation.Core.Presentation
{
    /// <summary>Сессия, результат которой можно сохранить в файл (используется экспортом).</summary>
    public interface IExportableSession
    {
        /// <summary>Название эксперимента для сохранения.</summary>
        string ExperimentName { get; }
        /// <summary>Полный запрос расчёта с исходными параметрами.</summary>
        SimulationRequest Request { get; }
        /// <summary>Полный результат расчёта, включая точки после окончания показа.</summary>
        SimulationResult Result { get; }
    }

    /// <summary>
    /// Прогон модели Обербека + вся геометрия показа: из решения [omega, phi] строит
    /// положения фигур сцены и значения выходных величин.
    /// Единицы сцены условные: шкив нарисован радиусом PulleyR, нить сматывается как PulleyR * deltaPhi.
    /// </summary>
    internal sealed class OberbeckSession : ISimulationSession, IExportableSession
    {
        // ---- геометрия сцены (условные единицы) ----
        private const double PulleyR = 0.5;
        private const double SpokeHalf = 1.8;
        private const double SpokeThickness = 0.09;
        private const double WeightDistance = 1.35;
        private const double WeightRadius = 0.16;
        private const double HubDiameter = 0.3;
        private const double LoadStartY = -2.0;
        private const double FloorY = -5.0;
        private const double RisingCeiling = -0.3;
        private const double ThreadWidth = 0.035;
        private const double LoadBaseSize = 0.55;

        // индексы фигур (порядок в Elements)
        private const int IPost = 0, IBase = 1, IPulleyOutline = 2, IPulley = 3, IMark = 4,
            ISpokeH = 5, ISpokeV = 6, IWeight0 = 7, IHub = 11, ILoad1 = 12, ILoad2 = 13,
            IThread1 = 14, IThread2 = 15, ElementCount = 16;

        private readonly double _mass1, _mass2, _radius, _torque, _alpha;
        private readonly double[] _times, _omega, _phi;
        private readonly double _t0;
        private readonly double _size1, _size2;
        private readonly bool _hasLoad2;
        private readonly double _duration;
        private readonly string _finishMessage;
        private readonly IReadOnlyList<OutputInfo> _outputs;
        private readonly SceneDescription _scene;

        /// <summary>Создаёт экземпляр с указанными свойствами.</summary>
        public OberbeckSession(OberbeckParameters parameters, double gravity,
            SimulationRequest request, SimulationResult result, IReadOnlyList<OutputInfo> outputs)
        {
            Request = request;
            Result = result;
            _outputs = outputs;

            _mass1 = parameters.Mass1;
            _mass2 = parameters.Mass2;
            _radius = parameters.Radius;
            _torque = parameters.CalculateTorque(gravity);
            _alpha = parameters.MomentOfInertia > 0.0 ? _torque / parameters.MomentOfInertia : 0.0;

            var states = result.States;
            var times = result.Times;
            int n = states.Length;
            if (n == 0) throw new InvalidOperationException("Simulation produced no data.");
            _times = new double[n];
            _omega = new double[n];
            _phi = new double[n];
            for (int i = 0; i < n; i++)
            {
                _times[i] = times[i];
                _omega[i] = states[i][0];
                _phi[i] = states[i][1];
            }
            _t0 = _times[0];

            // размеры грузов растут с массой (относительно большего груза)
            double mMax = Math.Max(_mass1, _mass2);
            _size1 = LoadSize(_mass1, mMax);
            _size2 = LoadSize(_mass2, mMax);
            _hasLoad2 = _mass2 > 1e-9;

            _scene = BuildScene();

            // сколько показывать: до конца расчёта или до касания основания
            _duration = _times[n - 1] - _t0;
            _finishMessage = "Simulation complete.";
            for (int i = 1; i < n; i++)
            {
                if (!Exceeds(_phi[i] - _phi[0])) continue;

                double lo = _times[i - 1], hi = _times[i];
                for (int k = 0; k < 50; k++)
                {
                    double mid = 0.5 * (lo + hi);
                    double o, p;
                    Sample(mid, out o, out p);
                    if (Exceeds(p - _phi[0])) hi = mid; else lo = mid;
                }
                _duration = hi - _t0;
                _finishMessage = "A load reached the limit of its travel.";
                break;
            }
        }

        /// <summary>Полный запрос расчёта с исходными параметрами.</summary>
        public SimulationRequest Request { get; }
        /// <summary>Полный результат расчёта, включая точки после окончания показа.</summary>
        public SimulationResult Result { get; }
        /// <summary>Название эксперимента для сохранения.</summary>
        public string ExperimentName { get { return "Обербек"; } }

        /// <summary>Описание фигур и границ сцены.</summary>
        public SceneDescription Scene { get { return _scene; } }
        /// <summary>Описания выходных величин в порядке значений кадра.</summary>
        public IReadOnlyList<OutputInfo> Outputs { get { return _outputs; } }
        public double Duration { get { return _duration; } }
        public string FinishMessage { get { return _finishMessage; } }

        public SimulationFrame GetFrame(double time)
        {
            Guard.Finite(time, nameof(time));
            double elapsed = Math.Min(Math.Max(time, 0.0), _duration);
            double t = _t0 + elapsed;

            double omega, phi;
            Sample(t, out omega, out phi);
            double dPhi = phi - _phi[0];

            return new SimulationFrame(t, BuildPoses(phi, dPhi), BuildValues(t, omega, phi, dPhi));
        }

        // ---------------- значения ----------------

        // порядок совпадает с OberbeckModelDescriptor.OutputList
        private double[] BuildValues(double t, double omega, double phi, double dPhi)
        {
            return new[]
            {
                t,
                omega,
                phi,
                _radius * dPhi,
                _radius * omega,
                _torque,
                _alpha
            };
        }

        // ---------------- сцена ----------------

        private static double LoadSize(double mass, double mMax)
        {
            double ratio = mMax > 0.0 ? mass / mMax : 1.0;
            double k = Math.Pow(Math.Max(ratio, 1e-6), 1.0 / 3.0);
            return LoadBaseSize * Math.Min(Math.Max(k, 0.6), 1.0);
        }

        private static SceneDescription BuildScene()
        {
            var stand = new Rgba(0.55f, 0.57f, 0.60f, 1f);
            var outline = new Rgba(0.12f, 0.20f, 0.38f, 1f);
            var pulley = new Rgba(0.25f, 0.45f, 0.75f, 1f);
            var cross = new Rgba(0.25f, 0.27f, 0.30f, 1f);
            var weight = new Rgba(0.80f, 0.60f, 0.15f, 1f);
            var marked = new Rgba(0.85f, 0.20f, 0.20f, 1f);

            var e = new SceneElement[ElementCount];
            e[IPost] = new SceneElement("post", ShapeKind.Rectangle, stand, 0);
            e[IBase] = new SceneElement("base", ShapeKind.Rectangle, stand, 0);
            e[IPulleyOutline] = new SceneElement("pulleyOutline", ShapeKind.Circle, outline, 2);
            e[IPulley] = new SceneElement("pulley", ShapeKind.Circle, pulley, 3);
            e[IMark] = new SceneElement("pulleyMark", ShapeKind.Rectangle, new Rgba(1f, 1f, 1f, 1f), 4);
            e[ISpokeH] = new SceneElement("spokeHorizontal", ShapeKind.Rectangle, cross, 5);
            e[ISpokeV] = new SceneElement("spokeVertical", ShapeKind.Rectangle, cross, 5);
            for (int i = 0; i < 4; i++)
                e[IWeight0 + i] = new SceneElement("spokeWeight" + (i + 1), ShapeKind.Circle, i == 0 ? marked : weight, 6);
            e[IHub] = new SceneElement("hub", ShapeKind.Circle, cross, 7);
            e[ILoad1] = new SceneElement("load1", ShapeKind.Rectangle, new Rgba(0.65f, 0.32f, 0.12f, 1f), 3);
            e[ILoad2] = new SceneElement("load2", ShapeKind.Rectangle, new Rgba(0.20f, 0.55f, 0.35f, 1f), 3);
            e[IThread1] = new SceneElement("thread1", ShapeKind.Rectangle, new Rgba(0.1f, 0.1f, 0.1f, 1f), 3);
            e[IThread2] = new SceneElement("thread2", ShapeKind.Rectangle, new Rgba(0.1f, 0.1f, 0.1f, 1f), 3);

            return new SceneDescription(e, -2.9, 2.9, FloorY - 0.4, SpokeHalf + 0.3, new Rgba(0.94f, 0.95f, 0.97f, 1f));
        }

        private ElementPose[] BuildPoses(double phi, double dPhi)
        {
            // phi > 0 - вращение по часовой стрелке; в сцене угол против часовой => знак минус
            double c = Math.Cos(-phi), s = Math.Sin(-phi);
            double theta = -phi * 180.0 / Math.PI;

            var p = new ElementPose[ElementCount];

            double h = -FloorY;
            p[IPost] = new ElementPose(0, FloorY + h * 0.5, 0, 0.22, h, true);
            p[IBase] = new ElementPose(0, FloorY - 0.1, 0, 5.5, 0.2, true);

            p[IPulleyOutline] = new ElementPose(0, 0, 0, PulleyR * 2 + 0.06, PulleyR * 2 + 0.06, true);
            p[IPulley] = new ElementPose(0, 0, 0, PulleyR * 2, PulleyR * 2, true);
            p[IMark] = Rotated(PulleyR * 0.7, 0, theta, PulleyR * 0.35, 0.06, c, s);

            p[ISpokeH] = new ElementPose(0, 0, theta, SpokeHalf * 2, SpokeThickness, true);
            p[ISpokeV] = new ElementPose(0, 0, theta, SpokeThickness, SpokeHalf * 2, true);

            // 4 груза на спицах: вправо, вниз, влево, вверх (в исходном положении)
            double[] dx = { 1, 0, -1, 0 };
            double[] dy = { 0, -1, 0, 1 };
            for (int i = 0; i < 4; i++)
                p[IWeight0 + i] = Rotated(dx[i] * WeightDistance, dy[i] * WeightDistance, 0, WeightRadius * 2, WeightRadius * 2, c, s);

            p[IHub] = new ElementPose(0, 0, 0, HubDiameter, HubDiameter, true);

            // грузы: + dPhi => m1 опускается, m2 поднимается
            double drop = PulleyR * dPhi;
            double up = MaxDrop(1), down = MaxDrop(-1);
            if (drop > up) drop = up;
            if (drop < -down) drop = -down;

            double y1 = LoadStartY - drop;
            double y2 = LoadStartY + drop;

            p[ILoad1] = new ElementPose(PulleyR, y1, 0, _size1, _size1, true);
            p[ILoad2] = new ElementPose(-PulleyR, y2, 0, _size2, _size2, _hasLoad2);
            p[IThread1] = Thread(PulleyR, y1 + _size1 * 0.5, true);
            p[IThread2] = Thread(-PulleyR, y2 + _size2 * 0.5, _hasLoad2);
            return p;
        }

        // поворот точки (x, y) вокруг центра шкива на cos/sin
        private static ElementPose Rotated(double x, double y, double theta, double w, double h, double c, double s)
        {
            return new ElementPose(x * c - y * s, x * s + y * c, theta, w, h, true);
        }

        // нить - вертикальный прямоугольник от точки касания шкива (y = 0) до верха груза
        private static ElementPose Thread(double x, double yBottom, bool visible)
        {
            double length = Math.Max(0.0, -yBottom);
            return new ElementPose(x, yBottom * 0.5, 0, ThreadWidth, length, visible);
        }

        // ---------------- пределы хода ----------------

        // максимальный путь нити (ед. сцены), пока груз не коснётся основания,
        // а поднимающийся - не дойдёт до шкива. direction >= 0: опускается m1.
        private double MaxDrop(int direction)
        {
            double down1 = LoadStartY - FloorY - _size1 * 0.5;
            double down2 = LoadStartY - FloorY - _size2 * 0.5;
            double up1 = (RisingCeiling - _size1 * 0.5) - LoadStartY;
            double up2 = (RisingCeiling - _size2 * 0.5) - LoadStartY;

            if (direction >= 0)
                return _hasLoad2 ? Math.Min(down1, up2) : down1;
            return _hasLoad2 ? Math.Min(down2, up1) : up1;
        }

        private bool Exceeds(double dPhi)
        {
            int dir = dPhi >= 0.0 ? 1 : -1;
            return PulleyR * Math.Abs(dPhi) >= MaxDrop(dir);
        }

        // ---------------- интерполяция решения ----------------

        private void Sample(double t, out double omega, out double phi)
        {
            int n = _times.Length;
            int i = Array.BinarySearch(_times, t);
            if (i >= 0)
            {
                omega = _omega[i];
                phi = _phi[i];
                return;
            }

            int hi = ~i;   // первый отсчёт, больший t
            if (hi <= 0)
            {
                omega = _omega[0];
                phi = _phi[0];
            }
            else if (hi >= n)
            {
                omega = _omega[n - 1];
                phi = _phi[n - 1];
            }
            else
            {
                int lo = hi - 1;
                double k = (t - _times[lo]) / (_times[hi] - _times[lo]);
                omega = _omega[lo] + (_omega[hi] - _omega[lo]) * k;
                phi = _phi[lo] + (_phi[hi] - _phi[lo]) * k;
            }
        }
    }
}
