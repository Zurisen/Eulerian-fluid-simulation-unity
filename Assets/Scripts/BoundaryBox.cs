using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BoundaryVisualizer : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private SimulationController _sm;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _sm = FindObjectOfType<SimulationController>();

        if (_sm != null)
        {
            DrawBoundary();
        }
    }

    void DrawBoundary()
    {
        _lineRenderer.positionCount = 5;
        _lineRenderer.useWorldSpace = true;

        Vector3[] boundaryPoints = new Vector3[5];
        boundaryPoints[0] = new Vector3(_sm.LeftBoundary-_sm.Size, _sm.TopBoundary+_sm.Size, 0);
        boundaryPoints[1] = new Vector3(_sm.RightBoundary+_sm.Size, _sm.TopBoundary+_sm.Size, 0);
        boundaryPoints[2] = new Vector3(_sm.RightBoundary+_sm.Size, _sm.BottomBoundary-_sm.Size, 0);
        boundaryPoints[3] = new Vector3(_sm.LeftBoundary-_sm.Size, _sm.BottomBoundary-_sm.Size, 0);
        boundaryPoints[4] = new Vector3(_sm.LeftBoundary-_sm.Size, _sm.TopBoundary+_sm.Size, 0); // Close the loop

        _lineRenderer.SetPositions(boundaryPoints);
        _lineRenderer.loop = true;
    }

    void OnValidate()
    {
        if (_lineRenderer != null && _sm != null)
        {
            DrawBoundary();
        }
    }
}
