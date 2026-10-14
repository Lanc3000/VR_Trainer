using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class LeverInteractable : XRBaseInteractable
{
    [SerializeField] private Vector3 _hingeAxis = Vector3.right;
    [SerializeField] private float _minAngle = -15f;
    [SerializeField] private float _maxAngle = 75f;
    [SerializeField, Range(0f, 1f)] private float _onThreshold = 0.9f;

    private float _angle;

    public UnityEvent OnLeverOn = new UnityEvent();
    public float NormalizedValue => Mathf.InverseLerp(_minAngle, _maxAngle, _angle);
    public bool IsOn { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        ResetLever();
    }

    private void Update()
    {
        if (IsOn || interactorsSelecting.Count == 0)
        {
            return;
        }

        Transform attachTransform = interactorsSelecting[0].GetAttachTransform(this);
        if (attachTransform == null)
        {
            return;
        }

        Vector3 axis = _hingeAxis.normalized;
        Vector3 hingeAxisWorld = transform.TransformDirection(axis);
        Vector3 projected = Vector3.ProjectOnPlane(
            attachTransform.position - transform.position, hingeAxisWorld);

        // Убираем текущий угол, чтобы отсчёт шёл от исходного положения pivot.
        Vector3 neutralUp = transform.TransformDirection(
            Quaternion.AngleAxis(-_angle, axis) * Vector3.up);
        Vector3 refUp = Vector3.ProjectOnPlane(neutralUp, hingeAxisWorld);
        if (projected.sqrMagnitude < 0.0001f || refUp.sqrMagnitude < 0.0001f)
        {
            return;
        }

        _angle = Mathf.Clamp(
            Vector3.SignedAngle(refUp, projected, hingeAxisWorld),
            _minAngle, _maxAngle);
        ApplyAngle();

        if (NormalizedValue >= _onThreshold)
        {
            _angle = _maxAngle;
            IsOn = true;
            ApplyAngle();
            OnLeverOn.Invoke();
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        // После отпускания оставляем текущий угол.
    }

    public void ResetLever()
    {
        _angle = _minAngle;
        IsOn = false;
        ApplyAngle();
    }

    private void ApplyAngle()
    {
        transform.localRotation = Quaternion.AngleAxis(_angle, _hingeAxis.normalized);
    }
}
