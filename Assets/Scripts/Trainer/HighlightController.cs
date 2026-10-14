using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HighlightController : MonoBehaviour
{
    [SerializeField] private Color _highlightColor = Color.yellow;

    private readonly List<Material> _materials = new List<Material>();
    private readonly List<Color> _originalEmissionColors = new List<Color>();
    private readonly List<bool> _originalEmissionKeywords = new List<bool>();
    private readonly List<MeshRenderer> _previewRenderers = new List<MeshRenderer>();
    private readonly List<Material[]> _previewOriginalMaterials = new List<Material[]>();
    private readonly List<Material> _previewMaterials = new List<Material>();
    [System.NonSerialized] private MeshRenderer[] _meshRenderers;
    [System.NonSerialized] private bool _isHighlighted;
    [System.NonSerialized] private bool _isEditModePreview;

    private void Awake()
    {
        _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        if (Application.isPlaying)
        {
            CacheMaterials();
        }
    }

    private void Update()
    {
        if (!_isHighlighted)
        {
            return;
        }

        ApplyIntensity(2f + Mathf.Sin(Time.time * 3f));
    }

    private void ApplyIntensity(float intensity)
    {
        foreach (Material material in _materials)
        {
            material.SetColor("_EmissionColor", _highlightColor * intensity);
        }
    }

    private void OnDisable()
    {
        if (_materials.Count > 0)
        {
            SetHighlighted(false);
        }
    }

    public void SetHighlighted(bool on)
    {
        if (_materials.Count == 0)
        {
            if (!on)
            {
                _isHighlighted = false;
                return;
            }

            CacheMaterials();
            if (_materials.Count == 0)
            {
                _isHighlighted = false;
                return;
            }
        }

        _isHighlighted = on;
        for (int i = 0; i < _materials.Count; i++)
        {
            Material material = _materials[i];
            if (on)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.SetColor("_EmissionColor", _originalEmissionColors[i]);
                if (_originalEmissionKeywords[i])
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }
        }

        if (on)
        {
            ApplyIntensity(3f);
        }

#if UNITY_EDITOR
        if (_isEditModePreview)
        {
            EditorApplication.update -= UpdateEditorPreview;
            if (on)
            {
                EditorApplication.update += UpdateEditorPreview;
            }

            SceneView.RepaintAll();
        }
#endif

        if (!on && _isEditModePreview)
        {
            RestoreEditorPreview();
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
    }

    [ContextMenu("Test Toggle")]
    private void TestToggle()
    {
        bool turnOn = !_isHighlighted;
        int materialCount = _materials.Count;
        SetHighlighted(turnOn);
        if (turnOn && !_isHighlighted)
        {
            return;
        }

        if (turnOn)
        {
            materialCount = _materials.Count;
        }

        Debug.Log($"HighlightController: подсветка {(_isHighlighted ? "включена" : "выключена")}, материалов: {materialCount}.", this);
    }

    private void CacheMaterials()
    {
        if (_meshRenderers == null || !Application.isPlaying)
        {
            _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        }

        _isEditModePreview = !Application.isPlaying;
        int materialSlots = 0;
        foreach (MeshRenderer meshRenderer in _meshRenderers)
        {
            Material[] materials = _isEditModePreview
                ? meshRenderer.sharedMaterials
                : meshRenderer.materials;
            Material[] originalMaterials = _isEditModePreview ? (Material[])materials.Clone() : null;
            bool hasPreviewMaterials = false;
            materialSlots += materials.Length;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                if (_isEditModePreview)
                {
                    // В редакторе меняем только временную копию материала.
                    material = new Material(material)
                    {
                        hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                    };
                    materials[i] = material;
                    _previewMaterials.Add(material);
                    hasPreviewMaterials = true;
                }

                _materials.Add(material);
                _originalEmissionColors.Add(material.GetColor("_EmissionColor"));
                _originalEmissionKeywords.Add(material.IsKeywordEnabled("_EMISSION"));
            }

            if (hasPreviewMaterials)
            {
                _previewRenderers.Add(meshRenderer);
                _previewOriginalMaterials.Add(originalMaterials);
                meshRenderer.sharedMaterials = materials;
            }
        }

        if (_materials.Count == 0)
        {
            _isEditModePreview = false;
            Debug.LogWarning($"HighlightController на {name}: найдено MeshRenderer: {_meshRenderers.Length}, слотов материалов: {materialSlots}, материалов с _EmissionColor: 0.", this);
            return;
        }

    }

#if UNITY_EDITOR
    private void UpdateEditorPreview()
    {
        if (!_isHighlighted || !_isEditModePreview)
        {
            EditorApplication.update -= UpdateEditorPreview;
            return;
        }

        ApplyIntensity(2f + Mathf.Sin((float)EditorApplication.timeSinceStartup * 3f));
        SceneView.RepaintAll();
    }
#endif

    private void RestoreEditorPreview()
    {
        for (int i = 0; i < _previewRenderers.Count; i++)
        {
            if (_previewRenderers[i] != null)
            {
                _previewRenderers[i].sharedMaterials = _previewOriginalMaterials[i];
            }
        }

        foreach (Material material in _previewMaterials)
        {
            DestroyImmediate(material);
        }

        _previewRenderers.Clear();
        _previewOriginalMaterials.Clear();
        _previewMaterials.Clear();
        _materials.Clear();
        _originalEmissionColors.Clear();
        _originalEmissionKeywords.Clear();
        _isEditModePreview = false;
    }
}
