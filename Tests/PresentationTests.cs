using System;
using System.Collections.Generic;
using System.Linq;
using OberbekRotation.Core.Presentation;
using OberbekRotation.Export;
using Xunit;

namespace OberbekRotation.Tests
{
    public class PresentationTests
    {
        [Fact]
        public void DefaultSessionExportsFullLabResultEvenAfterSceneStops()
        {
            var session = new OberbeckModelDescriptor().Start(null);
            var exportable = Assert.IsAssignableFrom<IExportableSession>(session);
            var experiment = new Experiment(exportable.ExperimentName, exportable.Request, exportable.Result);
            Assert.Equal(4.905, experiment.Result.States.Last()[0], 10);
            Assert.Equal(2, experiment.Result.Times.Last());
            Assert.InRange(session.Duration, 0.01, 1.99);
            Assert.Equal(session.Scene.Elements.Count, session.GetFrame(0).Poses.Length);
            Assert.Equal(session.Outputs.Count, session.GetFrame(0).Values.Length);
        }

        [Theory]
        [InlineData(0.2, 0.15, 2.4525)]
        [InlineData(0.15, 0.2, -2.4525)]
        [InlineData(0.2, 0.2, 0)]
        public void FramesMatchCalculationAndMotionDirection(double mass1, double mass2, double alpha)
        {
            var session = new OberbeckModelDescriptor().Start(new Dictionary<string, double>
            {
                ["mass1"] = mass1, ["mass2"] = mass2, ["endTime"] = 1
            });
            var initial = session.GetFrame(0);
            var frame = session.GetFrame(1);
            Assert.Equal(alpha, frame.Values[1], 10);
            Assert.Equal(alpha / 2, frame.Values[2], 10);
            Assert.Equal(0.05 * alpha / 2, frame.Values[3], 10);
            Assert.Equal(0.05 * alpha, frame.Values[4], 10);
            Assert.Equal(alpha, frame.Values[6], 10);
            int load = session.Scene.Elements.ToList().FindIndex(e => e.Id == "load1");
            Assert.Equal(Math.Sign(alpha), Math.Sign(initial.Poses[load].Y - frame.Poses[load].Y));
        }

        [Fact]
        public void FrameTimeClampsAndNonFiniteTimeIsRejected()
        {
            var session = new OberbeckModelDescriptor().Start(null);
            Assert.Equal(session.GetFrame(0).Values, session.GetFrame(-10).Values);
            Assert.Equal(session.GetFrame(session.Duration).Values, session.GetFrame(10).Values);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.GetFrame(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.GetFrame(double.PositiveInfinity));
        }

        [Theory]
        [InlineData("mass1", -1)]
        [InlineData("radius", double.NaN)]
        [InlineData("gravity", 0)]
        [InlineData("timeStep", double.PositiveInfinity)]
        public void InvalidPresentationParametersFail(string key, double value)
        {
            Assert.Throws<ArgumentException>(() => new OberbeckModelDescriptor().Start(
                new Dictionary<string, double> { [key] = value }));
        }

        [Fact]
        public void FineSamplingProducesUsableFrames()
        {
            // This also exercises session construction with defensive result copies.
            var session = new OberbeckModelDescriptor().Start(new Dictionary<string, double>
            {
                ["timeStep"] = 0.0001, ["endTime"] = 1
            });
            Assert.Equal(2.4525, session.GetFrame(1).Values[1], 9);
            Assert.Equal(10001, ((IExportableSession)session).Result.Count);
        }
    }
}
