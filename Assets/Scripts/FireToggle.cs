using UnityEngine;

/// <summary>
/// Lights or extinguishes a fire made of particle systems with a flickering light. Call <see cref="Toggle"/> from a
/// UI button or an interactable's select event.
/// </summary>
public class FireToggle : MonoBehaviour
{
    [Tooltip("Particle systems of the flame and sparks.")]
    public ParticleSystem[] fires;
    [Tooltip("Warm light that lights the room while the fire burns.")]
    public Light fireLight;
    [Tooltip("Optional crackle loop that plays while the fire burns.")]
    public AudioSource crackle;
    public float baseIntensity = 2.2f;
    public bool burning = true;

    private float target;

    private void Start() { Apply(burning, true); }

    /// <summary>Switches the fire on or off.</summary>
    public void Toggle() { Apply(!burning, false); }

    /// <summary>Select event handler for an XR interactable (pointing at it and pulling the trigger).</summary>
    public void OnSelect(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args) { Toggle(); }

    /// <summary>Sets the fire state.</summary>
    public void Apply(bool on, bool instant)
    {
        burning = on;
        foreach (var f in fires)
        {
            if (f == null) continue;
            if (on) f.Play(true); else f.Stop(true, instant ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
        if (crackle != null) { if (on) crackle.Play(); else crackle.Stop(); }
    }

    private void Update()
    {
        if (fireLight == null) return;
        target = burning ? baseIntensity * (0.8f + 0.4f * Mathf.PerlinNoise(Time.time * 6f, 0.3f)) : 0f;
        fireLight.intensity = Mathf.Lerp(fireLight.intensity, target, Time.deltaTime * 6f);
        fireLight.enabled = fireLight.intensity > 0.02f;
    }
}
