using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundaryBox : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private SimulationController _sm;
    

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _sm = FindObjectOfType<SimulationController>();

        if (_sm == null) throw new Exception("No Simulation Controller available");

        DrawBoundary();
    }

    void DrawBoundary(){
        float left = _sm.LeftBoundary;
        float right = _sm.RightBoundary;
        float top = _sm.TopBoundary;
        float bottom = _sm.BottomBoundary;

        Vector3[] points = new Vector3[5];
        points[0] = new Vector3(left, top, 0);
        points[1] = new Vector3(right, top, 0);
        points[2] = new Vector3(right, bottom, 0);
        points[3] = new Vector3(left, bottom, 0);
        points[4] = new Vector3(left, top, 0); // Closing the rectangle

        _lineRenderer.positionCount = points.Length;
        _lineRenderer.SetPositions(points);
    }
}
