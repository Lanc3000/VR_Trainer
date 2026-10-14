using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
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
    [SerializeField, TextArea] private string _textFinished = "Тренировка завершена.";

    private TrainerStep _currentStep = TrainerStep.TakeObjectA;

    public TrainerStep CurrentStep => _currentStep;
    public event Action<TrainerStep> StepChanged;

    private void Awake()
    {
        _currentStep = TrainerStep.TakeObjectA;
        _objectA.selectEntered.AddListener(OnObjectAGrabbed);
        _socket.selectEntered.AddListener(OnObjectPlaced);
        _lever.OnLeverOn.AddListener(OnLeverSwitched);

        _socket.enabled = false;
        _lever.enabled = false;
    }

    private void Start()
    {
        ShowCurrentStep();
    }

    private void OnDestroy()
    {
        if (_objectA != null)
        {
            _objectA.selectEntered.RemoveListener(OnObjectAGrabbed);
        }

        if (_socket != null)
        {
            _socket.selectEntered.RemoveListener(OnObjectPlaced);
        }

        if (_lever != null)
        {
            _lever.OnLeverOn.RemoveListener(OnLeverSwitched);
        }
    }

    private void OnObjectAGrabbed(SelectEnterEventArgs args)
    {
        if (CurrentStep != TrainerStep.TakeObjectA)
        {
            return;
        }

        ChangeStep(TrainerStep.PlaceObjectA);
    }

    private void OnObjectPlaced(SelectEnterEventArgs args)
    {
        if (CurrentStep != TrainerStep.PlaceObjectA ||
            !ReferenceEquals(args.interactableObject, _objectA))
        {
            return;
        }

        ChangeStep(TrainerStep.SwitchLever);
    }

    private void OnLeverSwitched()
    {
        if (CurrentStep != TrainerStep.SwitchLever)
        {
            return;
        }

        ChangeStep(TrainerStep.Finished);
    }

    private void ChangeStep(TrainerStep nextStep)
    {
        _currentStep = nextStep;

        if (nextStep == TrainerStep.PlaceObjectA)
        {
            _socket.enabled = true;
        }
        else if (nextStep == TrainerStep.SwitchLever)
        {
            _lever.enabled = true;
        }

        ShowCurrentStep();
        StepChanged?.Invoke(nextStep);
    }

    private void ShowCurrentStep()
    {
        _highlightObjectA.SetHighlighted(false);
        _highlightObjectB.SetHighlighted(false);
        _highlightLever.SetHighlighted(false);

        switch (CurrentStep)
        {
            case TrainerStep.TakeObjectA:
                _highlightObjectA.SetHighlighted(true);
                _ui.Show(_textStep1);
                break;
            case TrainerStep.PlaceObjectA:
                _highlightObjectB.SetHighlighted(true);
                _ui.Show(_textStep2);
                break;
            case TrainerStep.SwitchLever:
                _highlightLever.SetHighlighted(true);
                _ui.Show(_textStep3);
                break;
            case TrainerStep.Finished:
                _ui.Show(_textFinished);
                break;
        }
    }
}
