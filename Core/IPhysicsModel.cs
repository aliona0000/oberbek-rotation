namespace OberbekRotation.Core
{
    /// <summary>Контракт системы ОДУ первого порядка; все величины выражаются в SI.</summary>
    public interface IPhysicsModel
    {
        /// <summary>Название физической модели.</summary>
        string Name { get; }
        /// <summary>Число компонентов вектора состояния.</summary>
        int StateSize { get; }
        /// <summary>Возвращает новый массив производных, не изменяя входной state.</summary>
        double[] GetDerivatives(double time, double[] state);
    }
}
