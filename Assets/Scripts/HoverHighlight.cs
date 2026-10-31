using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Gives visual feedback on an interactable: a tint while a hand hovers over it and a stronger
/// tint while it is held. Uses a MaterialPropertyBlock so shared materials are not modified.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class HoverHighlight : MonoBehaviour
{
    [Tooltip("Tint while hovered.")]
    public Color hoverColor = new Color(0.35f, 0.85f, 1f);
    [Tooltip("Tint while held.")]
    public Color selectColor = new Color(0.5f, 1f, 0.5f);
    [Range(0f, 1f)]
    [Tooltip("How strongly the tint replaces the original colour.")]
    public float strength = 0.45f;

    private XRBaseInteractable interactable;
    private Renderer[] renderers;
    private Color[] original;
    private MaterialPropertyBlock block;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        original = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            original[i] = renderers[i].sharedMaterial != null && renderers[i].sharedMaterial.HasProperty(BaseColor)
                ? renderers[i].sharedMaterial.GetColor(BaseColor) : Color.white;
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(Refresh);
        interactable.hoverExited.AddListener(Refresh);
        interactable.selectEntered.AddListener(Refresh);
        interactable.selectExited.AddListener(Refresh);
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(Refresh);
        interactable.hoverExited.RemoveListener(Refresh);
        interactable.selectEntered.RemoveListener(Refresh);
        interactable.selectExited.RemoveListener(Refresh);
        Apply(null);
    }

    private void Refresh(UnityEngine.XR.Interaction.Toolkit.HoverEnterEventArgs _) { Refresh(); }
    private void Refresh(UnityEngine.XR.Interaction.Toolkit.HoverExitEventArgs _) { Refresh(); }
    private void Refresh(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _) { Refresh(); }
    private void Refresh(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _) { Refresh(); }

    private void Refresh()
    {
        if (interactable.isSelected) Apply(selectColor);
        else if (interactable.isHovered) Apply(hoverColor);
        else Apply(null);
    }

    private void Apply(Color? tint)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (tint.HasValue)
            {
                block.SetColor(BaseColor, Color.Lerp(original[i], tint.Value, strength));
                renderers[i].SetPropertyBlock(block);
            }
            else renderers[i].SetPropertyBlock(null);
        }
    }
}
