using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Particle : MonoBehaviour
{
    private SimulationController _sm;

    private Vector2 _velocity = Vector2.zero;


    // Start is called before the first frame update
    void Start()
    {
        _sm = FindObjectOfType<SimulationController>();
    }

    // Update is called once per frame
    void Update()
    {
        _velocity += new Vector2(0, _sm.Gravity * Time.deltaTime);
        transform.Translate(_velocity*Time.deltaTime);
        ResolveBoundariesCollision();
    }

    void ResolveBoundariesCollision(){
        if (_sm == null) return;

        Vector3 position = transform.position;
        if (position.x > _sm.RightBoundary)
        {
            position.x = _sm.RightBoundary;
            _velocity.x = -_velocity.x; // Reverse velocity on collision
        }
        else if (position.x < _sm.LeftBoundary)
        {
            position.x = _sm.LeftBoundary;
            _velocity.x = -_velocity.x; // Reverse velocity on collision
        }

        if (position.y > _sm.TopBoundary)
        {
            position.y = _sm.TopBoundary;
            _velocity.y = -_velocity.y; // Reverse velocity on collision
        }
        else if (position.y < _sm.BottomBoundary)
        {
            position.y = _sm.BottomBoundary;
            _velocity.y = -_velocity.y; // Reverse velocity on collision
        }

        transform.position = position;
    }
}
