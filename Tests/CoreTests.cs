using System;
using System.Linq;
using OberbekRotation.Core;
using Xunit;

namespace OberbekRotation.Tests
{
    public class CoreTests
    {
        [Fact]
        public void LabContractReturnsAccelerationThenOmega()
        {
            var model = new OberbeckModel(0.05, 0.1);
            Assert.Equal("Обербек", model.Name);
            Assert.Equal(2, model.StateSize);
            Assert.Equal(new[] { 2.0, -3.0 }, model.GetDerivatives(7, new[] { -3.0, 9.0 }));
        }

        [Theory]
        [InlineData(0.2, 0.15, 4.905)]
        [InlineData(0.15, 0.2, -4.905)]
        [InlineData(0.2, 0.2, 0)]
        public void LabMassesProduceCorrectSignedMotion(double mass1, double mass2, double omega)
        {
            var input = new SimulationRequest(new OberbeckParameters(mass1, mass2, 0.05, 0.01), 2);
            var result = new OberbeckSimulation().Run(input);
            Assert.Equal(201, result.Count);
            Assert.Equal(omega, result.States.Last()[0], 10);
            Assert.Equal(omega, result.States.Last()[1], 10);
            Assert.Equal(new[] { 0.0, 0.0 }, result.States[0]);
        }

        [Theory]
        [InlineData(0.0245, 0.3)]
        [InlineData(-0.0245, 0.1)]
        [InlineData(0, 4)]
        public void EverySampleMatchesAnalyticSolutionIncludingNonzeroInitialState(double torque, double dt)
        {
            var model = new OberbeckModel(0.01, torque);
            var result = new SimulationEngine(new Rk4Solver(), model).Run(new[] { 3.0, -2.0 }, 5, 7, dt);
            var states = result.States;
            var times = result.Times;
            Assert.Equal(5, times[0]);
            Assert.Equal(7, times.Last());
            for (int i = 0; i < times.Length; i++)
            {
                double t = times[i] - 5;
                Assert.Equal(3 + torque / 0.01 * t, states[i][0], 10);
                Assert.Equal(-2 + 3 * t + 0.5 * torque / 0.01 * t * t, states[i][1], 10);
                if (i > 0) Assert.True(times[i] > times[i - 1]);
            }
        }

        [Fact]
        public void Rk4HasFourthOrderConvergenceOnNonconstantDerivative()
        {
            var engine = new SimulationEngine(new Rk4Solver(), new ExponentialModel());
            double coarse = Math.Abs(engine.Run(new[] { 1.0 }, 0, 1, 0.2).States.Last()[0] - Math.E);
            double fine = Math.Abs(engine.Run(new[] { 1.0 }, 0, 1, 0.1).States.Last()[0] - Math.E);
            Assert.InRange(coarse / fine, 14, 17);
        }

        [Fact]
        public void Rk4EvaluatesStagesAtCorrectTimes()
        {
            var actual = new Rk4Solver().Step(new CubicTimeModel(), 1, new[] { 0.0 }, 1);
            Assert.Equal(3.75, actual[0], 12); // Integral of t^3 from 1 to 2.
        }

        [Fact]
        public void EngineClipsLastStepAndDoesNotOvershoot()
        {
            var result = new SimulationEngine(new Rk4Solver(), new OberbeckModel(1, 2))
                .Run(new[] { 0.0, 0.0 }, 0, 1, 0.3);
            Assert.Equal(5, result.Count);
            Assert.Equal(1, result.Times.Last());
            Assert.Equal(2, result.States.Last()[0], 12);
            Assert.Equal(1, result.States.Last()[1], 12);
        }

        [Fact]
        public void ZeroDurationReturnsOnlyInitialState()
        {
            var result = new SimulationEngine(new Rk4Solver(), new OberbeckModel(1, 2))
                .Run(new[] { 3.0, 4.0 }, 7, 7, 0.1);
            Assert.Single(result.Times);
            Assert.Equal(new[] { 3.0, 4.0 }, result.States[0]);
        }

        [Fact]
        public void InputAndOutputArraysCannotMutateStoredResult()
        {
            var initial = new[] { 1.0, 2.0 };
            var result = new SimulationEngine(new Rk4Solver(), new OberbeckModel(1, 2)).Run(initial, 0, 1, 0.5);
            Assert.Equal(new[] { 1.0, 2.0 }, initial);
            initial[0] = 999;
            result.Times[0] = 999;
            result.States[0][0] = 999;
            Assert.Equal(0, result.Times[0]);
            Assert.Equal(1, result.States[0][0]);
        }

        [Fact]
        public void BadInitialLengthFailsBeforeAnyDerivativeCall()
        {
            var model = new CountingModel();
            Assert.Throws<ArgumentException>(() => new SimulationEngine(new Rk4Solver(), model).Run(new[] { 0.0 }, 0, 2, 0.1));
            Assert.Equal(0, model.Calls);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void InvalidPositiveInputsFail(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OberbeckModel(value, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OberbeckParameters(1, 0, value, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationConfiguration(timeStep: value));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rk4Solver().Step(new OberbeckModel(1, 1), 0, new[] { 0.0, 0.0 }, value));
        }

        [Fact]
        public void InvalidRunAndNonFiniteDataFail()
        {
            var parameters = new OberbeckParameters(0.2, 0.15, 0.05, 0.01);
            Assert.Throws<ArgumentOutOfRangeException>(() => new OberbeckParameters(-1, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OberbeckModel(1, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRequest(parameters, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRequest(parameters, 2, maxSteps: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRequest(parameters, 2, initialOmega: double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRequest(parameters, 1e20 + 1e6, startTime: 1e20,
                configuration: new SimulationConfiguration(timeStep: 1), maxSteps: int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OberbeckModel(double.Epsilon, double.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationResult(new[] { 0.0 }, new[] { new[] { double.NaN } }));
            Assert.Throws<ArgumentException>(() => new SimulationResult(new[] { 1.0, 1.0 }, new[] { new[] { 0.0 }, new[] { 1.0 } }));
        }

        [Fact]
        public void InvalidDerivativeIsRejected()
        {
            var solver = new Rk4Solver();
            Assert.Throws<InvalidOperationException>(() => solver.Step(new BrokenModel(), 0, new[] { 1.0 }, 0.1));
        }

        [Fact]
        public void VectorForceAndPointMassUseRightHandedGeometry()
        {
            var force = new Force(new Vector(0, 5, 0), new Vector(2, 0, 0));
            Assert.Equal(new Vector(0, 0, 10), force.TorqueAbout(Vector.Zero));
            Assert.Equal(18, new Body(2, new Vector(3, 0, 100)).MomentOfInertia);
            Assert.Equal(32, new Vector(1, 2, 3).Dot(new Vector(4, 5, 6)));
        }

        private sealed class ExponentialModel : IPhysicsModel
        {
            public string Name => "exp";
            public int StateSize => 1;
            public double[] GetDerivatives(double time, double[] state) => new[] { state[0] };
        }
        private sealed class CubicTimeModel : IPhysicsModel
        {
            public string Name => "t^3";
            public int StateSize => 1;
            public double[] GetDerivatives(double time, double[] state) => new[] { time * time * time };
        }
        private sealed class CountingModel : IPhysicsModel
        {
            public int Calls;
            public string Name => "count";
            public int StateSize => 2;
            public double[] GetDerivatives(double time, double[] state) { Calls++; return new[] { 1.0, 1.0 }; }
        }
        private sealed class BrokenModel : IPhysicsModel
        {
            public string Name => "broken";
            public int StateSize => 1;
            public double[] GetDerivatives(double time, double[] state) => new[] { double.NaN };
        }
    }
}
