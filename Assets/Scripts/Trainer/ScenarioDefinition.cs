using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainerAction
{
    Grab,
    PlaceInSocket,
    SwitchLever
}

[Serializable]
public class TrainerStepDefinition
{
    [SerializeField] private TrainerAction _action;
    [SerializeField] private string _targetId;
    [SerializeField] private string _requiredObjectId;
    [SerializeField] private string _highlightId;
    [SerializeField, TextArea] private string _text;

    public TrainerAction Action => _action;
    public string TargetId => _targetId;
    public string RequiredObjectId => _requiredObjectId;
    public string HighlightId => string.IsNullOrEmpty(_highlightId) ? _targetId : _highlightId;
    public string Text => _text;

    public TrainerStepDefinition(
        TrainerAction action, string targetId, string requiredObjectId, string highlightId, string text)
    {
        _action = action;
        _targetId = targetId;
        _requiredObjectId = requiredObjectId;
        _highlightId = highlightId;
        _text = text;
    }
}

[CreateAssetMenu(fileName = "TrainerScenario", menuName = "Trainer/Scenario")]
public class ScenarioDefinition : ScriptableObject
{
    [SerializeField] private List<TrainerStepDefinition> _steps = new List<TrainerStepDefinition>();
    [SerializeField, TextArea] private string _finishedText = "Тренировка завершена. Нажмите R для перезапуска.";

    public IReadOnlyList<TrainerStepDefinition> Steps => _steps;
    public string FinishedText => _finishedText;
}
