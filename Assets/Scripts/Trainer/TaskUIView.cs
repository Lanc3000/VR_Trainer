using TMPro;
using UnityEngine;

public class TaskUIView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    public void Show(string message)
    {
        _text.text = message;
    }
}
