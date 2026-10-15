using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ScenarioController : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable _objectA;
    [SerializeField] private XRSocketInteractor _socket;
    [SerializeField] private LeverInteractable _lever;
    [SerializeField] private HighlightController _highlightObjectA;
    [SerializeField] private HighlightController _highlightObjectB;
    [SerializeField] private HighlightController _highlightLever;
    [SerializeField] private TaskUIView _ui;

    [SerializeField, TextArea] private string _textStep1 = "Возьмите объект A со стола.";
    [SerializeField, TextArea] private string _textStep2 = "Подойдите к объекту B и установите в него объект A.";
    [SerializeField, TextArea] private string _textStep3 = "Подойдите к рычагу и переведите его в положение «Включено».";
    [SerializeField, TextArea] private string _textFinished = "Тренировка завершена. Нажмите R для перезапуска.";

    [SerializeField] private ScenarioDefinition _scenario;
    [SerializeField] private List<ScenarioBinding> _bindings = new List<ScenarioBinding>();

    private readonly Dictionary<string, ScenarioBinding> _bindingsById =
        new Dictionary<string, ScenarioBinding>(StringComparer.Ordinal);
    private IReadOnlyList<TrainerStepDefinition> _steps;
    private IScenarioStepStrategy _activeStrategy;
    private HighlightController _activeHighlight;
    private int _currentStepIndex;
    private bool _isFinished;
    private TrainerStep _currentStep = TrainerStep.TakeObjectA;

    public int CurrentStepIndex => _currentStepIndex;
    public TrainerStepDefinition CurrentStepDefinition =>
        !_isFinished && _steps != null && _currentStepIndex < _steps.Count
            ? _steps[_currentStepIndex]
            : null;
    public bool IsFinished => _isFinished;

    // Совместимость с прежним API: значение отражает тип действия, не номер шага.
    public TrainerStep CurrentStep => _currentStep;
    public event Action<TrainerStep> StepChanged;
    public event Action<int> StepIndexChanged;

    private void Awake()
    {
        SetGateEnabled(_socket, false);
        SetGateEnabled(_lever, false);

        foreach (ScenarioBinding binding in _bindings)
        {
            if (binding != null)
            {
                SetGateEnabled(binding.Interactable, false);
            }
        }
    }

    private void Start()
    {
        try
        {
            if (_ui == null)
            {
                throw new InvalidOperationException("Не задан TaskUIView.");
            }

            BuildBindingMap();
            _steps = _scenario == null ? CreateDefaultSteps() : _scenario.Steps;
            if (_steps == null || _steps.Count == 0)
            {
                throw new InvalidOperationException("Сценарий не содержит шагов.");
            }

            foreach (TrainerStepDefinition step in _steps)
            {
                ValidateStep(step);
            }

            EnterCurrentStep(false);
        }
        catch (Exception exception)
        {
            Debug.LogError($"ScenarioController: {exception.Message}", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (_isFinished && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnDisable()
    {
        _activeStrategy?.Stop();
        _activeStrategy = null;
        _activeHighlight?.SetHighlighted(false);
        _activeHighlight = null;
    }

    private void BuildBindingMap()
    {
        _bindingsById.Clear();
        AddDefaultBinding("ObjectA", _objectA, _highlightObjectA);
        AddDefaultBinding("ObjectB", null, _highlightObjectB);
        AddDefaultBinding("SocketPoint", _socket, null);
        AddDefaultBinding("LeverPivot", _lever, _highlightLever);

        foreach (ScenarioBinding binding in _bindings)
        {
            AddBinding(binding);
        }
    }

    private void AddDefaultBinding(string id, Component interactable, HighlightController highlight)
    {
        if (interactable != null || highlight != null)
        {
            AddBinding(new ScenarioBinding(id, interactable, highlight));
        }
    }

    private void AddBinding(ScenarioBinding binding)
    {
        if (binding == null || string.IsNullOrWhiteSpace(binding.Id))
        {
            throw new InvalidOperationException("У привязки сцены отсутствует Id.");
        }

        if (_bindingsById.ContainsKey(binding.Id))
        {
            throw new InvalidOperationException($"Повторяется Id привязки '{binding.Id}'.");
        }

        _bindingsById.Add(binding.Id, binding);
    }

    private ScenarioBinding ResolveBinding(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !_bindingsById.TryGetValue(id, out ScenarioBinding binding))
        {
            throw new InvalidOperationException($"Не найдена привязка сцены '{id}'.");
        }

        return binding;
    }

    private List<TrainerStepDefinition> CreateDefaultSteps()
    {
        return new List<TrainerStepDefinition>
        {
            new TrainerStepDefinition(TrainerAction.Grab, "ObjectA", "", "ObjectA", _textStep1),
            new TrainerStepDefinition(TrainerAction.PlaceInSocket, "SocketPoint", "ObjectA", "ObjectB", _textStep2),
            new TrainerStepDefinition(TrainerAction.SwitchLever, "LeverPivot", "", "LeverPivot", _textStep3)
        };
    }

    private void ValidateStep(TrainerStepDefinition step)
    {
        if (step == null)
        {
            throw new InvalidOperationException("Сценарий содержит пустой шаг.");
        }

        if (ResolveBinding(step.HighlightId).Highlight == null)
        {
            throw new InvalidOperationException($"У шага '{step.Text}' не задан HighlightController.");
        }

        ScenarioStepStrategyFactory.Create(step, ResolveBinding);
    }

    private void EnterCurrentStep(bool notifyLegacy)
    {
        _activeHighlight?.SetHighlighted(false);
        _activeHighlight = null;

        if (_currentStepIndex >= _steps.Count)
        {
            FinishScenario();
            return;
        }

        TrainerStepDefinition step = _steps[_currentStepIndex];
        _currentStep = ToLegacyStep(step.Action);
        _activeHighlight = ResolveBinding(step.HighlightId).Highlight;
        _activeHighlight.SetHighlighted(true);
        _ui.Show(step.Text);

        _activeStrategy = ScenarioStepStrategyFactory.Create(step, ResolveBinding);
        _activeStrategy.Start(CompleteCurrentStep);

        StepIndexChanged?.Invoke(_currentStepIndex);
        if (notifyLegacy)
        {
            StepChanged?.Invoke(_currentStep);
        }
    }

    private void CompleteCurrentStep()
    {
        if (_activeStrategy == null || _isFinished)
        {
            return;
        }

        _activeStrategy.Stop();
        _activeStrategy = null;
        _currentStepIndex++;
        EnterCurrentStep(true);
    }

    private void FinishScenario()
    {
        _isFinished = true;
        _currentStep = TrainerStep.Finished;
        string finishedText = _scenario == null ? _textFinished : _scenario.FinishedText;
        _ui.Show(WithRestartHint(finishedText));
        StepIndexChanged?.Invoke(_currentStepIndex);
        StepChanged?.Invoke(TrainerStep.Finished);
    }

    private static string WithRestartHint(string text)
    {
        const string hint = " Нажмите R для перезапуска.";
        string message = string.IsNullOrWhiteSpace(text) ? "Тренировка завершена." : text;
        return message.EndsWith(hint, StringComparison.Ordinal) ? message : message + hint;
    }

    private static TrainerStep ToLegacyStep(TrainerAction action)
    {
        switch (action)
        {
            case TrainerAction.Grab:
                return TrainerStep.TakeObjectA;
            case TrainerAction.PlaceInSocket:
                return TrainerStep.PlaceObjectA;
            case TrainerAction.SwitchLever:
                return TrainerStep.SwitchLever;
            default:
                throw new InvalidOperationException($"Неизвестное действие шага: {action}.");
        }
    }

    private static void SetGateEnabled(Component component, bool on)
    {
        if (component is XRSocketInteractor socket)
        {
            socket.enabled = on;
        }
        else if (component is LeverInteractable lever)
        {
            lever.enabled = on;
        }
    }
}
