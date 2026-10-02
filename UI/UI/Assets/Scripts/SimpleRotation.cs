using UnityEngine;

public class SimpleRotation : MonoBehaviour
{
    [Header("Начальные условия")]
    public float initialOmega = 0f;   // рад/с
    public float initialPhi = 0f;     // рад

    [Header("Заглушка физики (пока нет Core)")]
    [Tooltip("Постоянное угловое ускорение для теста визуализации")]
    public float alphaDeg = 90f;      // град/с²

    private float omega;   // текущая угловая скорость, рад/с
    private float phi;     // текущий угол, рад
    private float time;

    private void Start()
    {
        omega = initialOmega;
        phi = initialPhi;
        time = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Когда появится Core — здесь будет:
        //   double[] state = new double[] { omega, phi };
        //   double[] deriv = model.GetDerivatives(time, state);
        //   omega += deriv[0] * dt;
        //   phi   += deriv[1] * dt;
        float alpha = alphaDeg * Mathf.Deg2Rad;
        omega += alpha * dt;
        phi += omega * dt;

        time += dt;

        // Применяем вращение к диску (вокруг оси Z, т.к. камера 2D)
        transform.rotation = Quaternion.Euler(0, 0, -phi * Mathf.Rad2Deg);
    }

    public float GetOmega() => omega;
    public float GetPhi() => phi;
    public float GetTime() => time;
}