using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Drives an analog wall clock and a digital read-out from the system clock.
/// The hands are rotated by code every frame (no animation clips are used).
/// The clock face is expected to look along its local +Z axis away from the viewer,
/// so a clockwise sweep is a negative rotation about local Z.
/// </summary>
public class AnalogClock : MonoBehaviour
{
    [Header("Analog hands")]
    [Tooltip("Pivot transform of the hour hand.")]
    public Transform hourHand;
    [Tooltip("Pivot transform of the minute hand.")]
    public Transform minuteHand;
    [Tooltip("Pivot transform of the second hand.")]
    public Transform secondHand;

    [Header("Digital read-out")]
    [Tooltip("Large HH:mm:ss text.")]
    public TMP_Text timeText;
    [Tooltip("Smaller AM/PM text.")]
    public TMP_Text periodText;
    [Tooltip("Date line, for example Monday, 26 October 2026.")]
    public TMP_Text dateText;

    [Header("Options")]
    [Tooltip("Sweep the second hand smoothly instead of ticking once per second.")]
    public bool smoothSecondHand = true;
    [Tooltip("Use 24 hour format on the digital display.")]
    public bool use24Hour = false;
    [Tooltip("Optional tick sound played once per second.")]
    public AudioSource tickSource;

    private int lastSecond = -1;

    private void Update()
    {
        DateTime now = DateTime.Now;
        float seconds = now.Second + (smoothSecondHand ? now.Millisecond / 1000f : 0f);
        float minutes = now.Minute + now.Second / 60f;
        float hours = (now.Hour % 12) + now.Minute / 60f;

        SetAngle(secondHand, seconds * 6f);   // 360 / 60
        SetAngle(minuteHand, minutes * 6f);   // 360 / 60
        SetAngle(hourHand, hours * 30f);      // 360 / 12

        UpdateDigital(now);

        if (now.Second != lastSecond)
        {
            lastSecond = now.Second;
            if (tickSource != null && tickSource.clip != null) tickSource.Play();
        }
    }

    private static void SetAngle(Transform hand, float degrees)
    {
        if (hand != null) hand.localRotation = Quaternion.Euler(0f, 0f, -degrees);
    }

    private void UpdateDigital(DateTime now)
    {
        if (timeText != null)
        {
            string colon = now.Second % 2 == 0 ? ":" : " ";
            int h = use24Hour ? now.Hour : (now.Hour % 12 == 0 ? 12 : now.Hour % 12);
            timeText.text = $"{h:00}{colon}{now.Minute:00}{colon}{now.Second:00}";
        }
        if (periodText != null) periodText.text = use24Hour ? string.Empty : (now.Hour < 12 ? "AM" : "PM");
        if (dateText != null) dateText.text = now.ToString("dddd, d MMMM yyyy");
    }
}
