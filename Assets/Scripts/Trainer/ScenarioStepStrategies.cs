using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public interface IScenarioStepStrategy
{
    void Start(Action onSuccess);
    void Stop();
}

public sealed class GrabStepStrategy : IScenarioStepStrategy
{
    private readonly XRGrabInteractable _target;
    private Action _onSuccess;

    public GrabStepStrategy(XRGrabInteractable target)
    {
        _target = target;
    }

    public void Start(Action onSuccess)
    {
        _onSuccess = onSuccess;
        _target.selectEntered.AddListener(OnSelected);
    }

    public void Stop()
    {
        _target.selectEntered.RemoveListener(OnSelected);
        _onSuccess = null;
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        _onSuccess?.Invoke();
    }
}

public sealed class SocketStepStrategy : IScenarioStepStrategy
{
    private readonly XRSocketInteractor _socket;
    private readonly XRBaseInteractable _requiredObject;
    private Action _onSuccess;

    public SocketStepStrategy(XRSocketInteractor socket, XRBaseInteractable requiredObject)
    {
        _socket = socket;
        _requiredObject = requiredObject;
    }

    public void Start(Action onSuccess)
    {
        _onSuccess = onSuccess;
        _socket.selectEntered.AddListener(OnSelected);
        _socket.enabled = true;
    }

    public void Stop()
    {
        _socket.selectEntered.RemoveListener(OnSelected);
        _onSuccess = null;
        // Сокет остаётся активным, чтобы удерживать установленный предмет.
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (ReferenceEquals(args.interactableObject, _requiredObject))
        {
            _onSuccess?.Invoke();
        }
    }
}

public sealed class LeverStepStrategy : IScenarioStepStrategy
{
    private readonly LeverInteractable _lever;
    private Action _onSuccess;

    public LeverStepStrategy(LeverInteractable lever)
    {
        _lever = lever;
    }

    public void Start(Action onSuccess)
    {
        _onSuccess = onSuccess;
        _lever.ResetLever();
        _lever.OnLeverOn.AddListener(OnLeverOn);
        _lever.enabled = true;
    }

    public void Stop()
    {
        _lever.OnLeverOn.RemoveListener(OnLeverOn);
        _onSuccess = null;
    }

    private void OnLeverOn()
    {
        _onSuccess?.Invoke();
    }
}

public static class ScenarioStepStrategyFactory
{
    public static IScenarioStepStrategy Create(
        TrainerStepDefinition step, Func<string, ScenarioBinding> resolve)
    {
        Component target = resolve(step.TargetId).Interactable;

        switch (step.Action)
        {
            case TrainerAction.Grab:
                return new GrabStepStrategy(Require<XRGrabInteractable>(target, step.TargetId));
            case TrainerAction.PlaceInSocket:
                Component required = resolve(step.RequiredObjectId).Interactable;
                return new SocketStepStrategy(
                    Require<XRSocketInteractor>(target, step.TargetId),
                    Require<XRBaseInteractable>(required, step.RequiredObjectId));
            case TrainerAction.SwitchLever:
                return new LeverStepStrategy(Require<LeverInteractable>(target, step.TargetId));
            default:
                throw new InvalidOperationException($"Неизвестное действие шага: {step.Action}.");
        }
    }

    private static T Require<T>(Component component, string id) where T : Component
    {
        if (component is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException(
            $"Привязка '{id}' должна содержать компонент {typeof(T).Name}.");
    }
}
