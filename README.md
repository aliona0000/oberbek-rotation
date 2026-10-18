# Моделирование вращения установки Обербека

Учебный проект на C# с физической моделью, численным интегрированием RK4,
экспортом результатов и интерфейсом Unity. Core и Export образуют слой Model
в архитектуре MVVM и не зависят от элементов интерфейса.

## Структура

| Папка | Назначение |
|---|---|
| [Core](Core) | Библиотека .NET Standard 2.0: физическая модель, проверка параметров, RK4 и результаты расчёта |
| [Export](Export) | Библиотека .NET Standard 2.0: сохранение и загрузка JSON, экспорт CSV |
| [UI](UI) | Проект Unity с интерфейсом и визуализацией |
| [Tests](Tests) | 55 автоматических тестов ядра, экспорта и кадров сцены |
| [Examples](Examples) | Консольный пример расчёта и сохранения файлов |
| [docs](docs/CORE-EXPORT.md) | Формулы, допущения, API, валидация и подключение библиотек |

## Модель

Используется упрощённая схема из лабораторной работы: две подвешенные массы
действуют на диск через канаты с одинаковым радиусом. Момент инерции I задан,
нить нерастяжима и не проскальзывает, трением пренебрегаем.

```text
tau = (m1 - m2) * g * R
state = [omega, phi]
d(omega)/dt = tau / I
d(phi)/dt = omega
```

Все величины в SI. Значение g по умолчанию равно 9.81 м/с².
Вычисления выполняет Rk4Solver, цикл моделирования организует SimulationEngine.

## Быстрый запуск

Для сборки библиотек, тестов и консольного примера нужен .NET 8 SDK.
Сами Core и Export совместимы с .NET Standard 2.0.

```powershell
dotnet restore OberbekRotation.sln --configfile NuGet.Config
dotnet test OberbekRotation.sln -c Release --no-restore
dotnet run --project Examples/OberbekRotation.Example.csproj -c Release
```

Пример создаёт `artifacts/example/oberbeck.json` и `oberbeck.csv`.
Для m1=0.200 кг, m2=0.150 кг, R=0.050 м и I=0.010 кг·м² ожидается:

```text
tau = 0.024525 Н·м
alpha = 2.4525 рад/с²
omega(2) = 4.905 рад/с
phi(2) = 4.905 рад
```

## Использование из ViewModel

```csharp
using OberbekRotation.Core;
using OberbekRotation.Export;

var parameters = new OberbeckParameters(0.200, 0.150, 0.050, 0.010);
var input = new SimulationRequest(parameters, endTime: 2);
var result = new OberbeckSimulation().Run(input);

double[] times = result.Times;
double[][] states = result.States; // [omega, phi] для каждого момента времени

var experiment = new Experiment("Обербек", input, result);
ExperimentFile.Save("experiment.json", experiment);
ExperimentFile.Save("experiment.csv", experiment);
var restored = ExperimentFile.Load("experiment.json");
```

ViewModel передаёт параметры, обрабатывает сообщения валидации и использует
результаты для графиков и анимации. Подключение этих библиотек к существующему
интерфейсу Unity выполняется отдельно в UI.

## Экспорт и проверка данных

- JSON сохраняет входные параметры и траекторию. После загрузки можно повторить расчёт.
- CSV содержит время, угловую скорость, угол, угловое ускорение и кинетическую энергию.
- Проверяются конечность чисел, допустимые массы, радиус, инерция, шаг,
  временной интервал, размеры массивов и структура JSON.
- XML-документация публичного API собирается вместе с DLL.

## DLL и NuGet

```powershell
dotnet build Export/Export.csproj -c Release
dotnet pack Core/Core.csproj -c Release -o artifacts/packages
dotnet pack Export/Export.csproj -c Release -o artifacts/packages
```

Результаты сборки:

- `Core/bin/Release/netstandard2.0/OberbekRotation.Core.dll`
- `Export/bin/Release/netstandard2.0/OberbekRotation.Export.dll`

Export использует Newtonsoft.Json 13.0.4. При ручном подключении DLL эта зависимость
также нужна. Подробности и контракт файлов описаны в [инструкции Core и Export](docs/CORE-EXPORT.md).

Готовые архивы, каталоги `bin/` и `obj/` в исходный код репозитория не включаются.
