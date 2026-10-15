using System;
using UnityEngine;

[Serializable]
public class ScenarioBinding
{
    [SerializeField] private string _id;
    [SerializeField] private Component _interactable;
    [SerializeField] private HighlightController _highlight;

    public string Id => _id;
    public Component Interactable => _interactable;
    public HighlightController Highlight => _highlight;

    public ScenarioBinding(string id, Component interactable, HighlightController highlight)
    {
        _id = id;
        _interactable = interactable;
        _highlight = highlight;
    }
}
