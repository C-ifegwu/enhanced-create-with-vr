using UnityEngine;

/// <summary>
/// Switches the projector effect on and off. Wired to the projector's interactable select event,
/// so pointing at it and pulling the trigger toggles the particles, the light and a sound.
/// </summary>
public class ProjectorToggle : MonoBehaviour
{
    [Tooltip("Particle system that draws the projector beam.")]
    public ParticleSystem particles;
    [Tooltip("Light that glows while the projector is on.")]
    public Light projectorLight;
    [Tooltip("Sound played when toggled.")]
    public AudioSource sound;

    private bool isOn;

    private void Start() { SetOn(false); }

    /// <summary>Toggles the projector.</summary>
    public void Toggle() { SetOn(!isOn); }

    private void SetOn(bool on)
    {
        isOn = on;
        if (particles != null)
        {
            if (on) particles.Play(); else particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (projectorLight != null) projectorLight.enabled = on;
        if (sound != null && sound.clip != null && Time.timeSinceLevelLoad > 0.5f) sound.Play();
    }
}
