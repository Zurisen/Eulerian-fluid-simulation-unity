using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public Vector2 Velocity = Vector2.zero;
    public float Mass {get; set;}
    public float Density {get; set;}
    public float Pressure {get; set;}

    
    [SerializeField]
    private SimulationController _sm;

    void Awake(){
        _sm = GetComponent<SimulationController>();
    }


    public void ChangeParticleSize(float size){
        transform.transform.localScale = new Vector2(size, size);

    }



    // Draw gizmos to visualize the particle in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x / 2);

        // Draw density and pressure
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;

        Handles.Label(transform.position + new Vector3(0, 0.5f, 0), $"Density: {Density:F2}", style);
        Handles.Label(transform.position + new Vector3(0, -0.5f, 0), $"Pressure: {Pressure:F2}", style);
        Handles.Label(transform.position + new Vector3(0, -1.0f, 0), $"Velocity: {Velocity:F2}", style);

        // Draw smoothing radius
        Gizmos.color = new Color(0, 1, 0, 0.25f); // Green with transparency
        Gizmos.DrawSphere(transform.position, _sm.SmoothingRadius);
    }
}
