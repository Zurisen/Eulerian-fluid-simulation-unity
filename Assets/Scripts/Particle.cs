using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public int Index { get; set; }
    public Vector2 Velocity = Vector2.zero;
    public float Mass;
    public float Density;
    public float Pressure;
    public Color PColor;
    public Color AuraColor;

    private SimulationController _sm;
    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _auraSpriteRenderer;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        PColor = _spriteRenderer.color;

        // Find the "Aura" child GameObject and get its SpriteRenderer component
        
        Transform auraTransform = transform.Find("Aura");
        if (auraTransform != null)
        {
            _auraSpriteRenderer = auraTransform.GetComponent<SpriteRenderer>();
            if (_auraSpriteRenderer != null)
            {
                Debug.Log("Aura SpriteRenderer found and assigned.");
            }

        }

    }


    public void ChangeParticleSize(float size)
    {
        transform.localScale = new Vector2(size, size);
        if (_auraSpriteRenderer != null)
        {
            _auraSpriteRenderer.transform.localScale = new Vector2(size * 2, size * 2);
        }
    }

    public void ChangeParticleAuraRadius(float radius)
    {
        if (_auraSpriteRenderer != null)
        {
            _auraSpriteRenderer.transform.localScale = new Vector2(radius, radius);
        }

    }

    public void UpdateParticleColor()
    {
        float maxSpeed = 5f; // Define a max speed for normalization
        float speed = Velocity.magnitude;
        float t = Mathf.Clamp01(speed / maxSpeed);
        Color newColor = Color.Lerp(PColor, Color.white, t);
        _spriteRenderer.color = newColor;

        // Optionally, update the aura color as well
        if (_auraSpriteRenderer != null)
        {
            _auraSpriteRenderer.color = new Color(newColor.r, newColor.g, newColor.b, 0.5f);
        }
    }

    public void ToggleAuraVisibility(bool isVisible)
    {
        if (_auraSpriteRenderer != null)
        {
            _auraSpriteRenderer.enabled = isVisible;
        }
    }

    // Draw gizmos to visualize the particle in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x / 2);

        // // Draw density and pressure
        // GUIStyle style = new GUIStyle();
        // style.normal.textColor = Color.white;

        // Handles.Label(transform.position + new Vector3(0, 0.5f, 0), $"Density: {Density:F2}", style);
        // Handles.Label(transform.position + new Vector3(0, -0.5f, 0), $"Pressure: {Pressure:F2}", style);
        // Handles.Label(transform.position + new Vector3(0, -1.0f, 0), $"Velocity: {Velocity:F2}", style);

        // // Draw smoothing radius
        // Gizmos.color = new Color(0, 1, 0, 0.25f); // Green with transparency
        // Gizmos.DrawSphere(transform.position, _sm.SmoothingRadius);
    }
}
