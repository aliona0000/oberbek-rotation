using System;
using UnityEngine;

namespace Assets.Scripts.View
{
    /// <summary>
    /// Проигрывает показ: двигает время от 0 до duration и на каждом шаге просит кадр
    /// у переданной функции (на деле - ViewModel.GetFrame) и показывает его на SceneView.
    /// Про содержимое кадров ничего не знает.
    /// </summary>
    public class FramePlayer : MonoBehaviour
    {
        public SceneView view;

        [Min(0.01f)] public float playbackSpeed = 1f;   // 1 = реальное время

        public event Action<FrameData> FrameShown;
        public event Action<string> Finished;

        public bool IsPlaying { get; private set; }

        private Func<double, FrameData> _getFrame;
        private double _duration;
        private string _finishMessage;
        private double _t;
        private bool _finished;

        /// <summary>Подготавливает показ и ставит первый кадр (без запуска).</summary>
        public void Load(double duration, Func<double, FrameData> getFrame, string finishMessage)
        {
            _duration = duration;
            _getFrame = getFrame;
            _finishMessage = finishMessage;
            Restart(false);
        }

        public void Play()
        {
            if (_getFrame == null) return;
            if (_finished) Restart(false);
            IsPlaying = true;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Restart(bool play)
        {
            if (_getFrame == null) return;
            _finished = false;
            _t = 0.0;
            Show(_t);
            IsPlaying = play;
        }

        private void Update()
        {
            if (!IsPlaying || _getFrame == null) return;

            _t += Time.deltaTime * playbackSpeed;
            bool end = _t >= _duration;
            if (end) _t = _duration;

            Show(_t);

            if (end)
            {
                IsPlaying = false;
                _finished = true;
                if (Finished != null) Finished(_finishMessage);
            }
        }

        private void Show(double t)
        {
            FrameData frame = _getFrame(t);
            if (frame == null) return;
            view.Apply(frame);
            if (FrameShown != null) FrameShown(frame);
        }
    }
}