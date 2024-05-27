using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public Vector2 Velocity = Vector2.zero;
    private SimulationController _sm;
    private float _radius;
    private Rigidbody2D _rigidbody2D;


    // Start is called before the first frame update
    void Start()
    {
        _sm = FindObjectOfType<SimulationController>();
        SetupParticleParams();
        _radius = GetComponent<CircleCollider2D>().radius * transform.localScale.x;
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }


    // Update is called once per frame
    void Update()
    {
        SetupParticleParams();
        Velocity += new Vector2(0, _sm.Gravity * Time.deltaTime);
        transform.Translate(Velocity*Time.deltaTime);
        ResolveBoundariesCollision();
    }


    void SetupParticleParams(){
        ChangeParticleSize(_sm.Size);
    }

    public void ChangeParticleSize(float size) {
        transform.transform.localScale = new Vector2(size, size);
    }


    void ResolveBoundariesCollision(){
        if (_sm == null) return;

        Vector3 position = transform.position;
        if (position.x > _sm.RightBoundary)
        {
            position.x = _sm.RightBoundary;
            Velocity.x = -Velocity.x; // Reverse velocity on collision
        }
        else if (position.x < _sm.LeftBoundary)
        {
            position.x = _sm.LeftBoundary;
            Velocity.x = -Velocity.x; // Reverse velocity on collision
        }

        if (position.y > _sm.TopBoundary)
        {
            position.y = _sm.TopBoundary;
            Velocity.y = -Velocity.y; // Reverse velocity on collision
        }
        else if (position.y < _sm.BottomBoundary)
        {
            position.y = _sm.BottomBoundary;
            Velocity.y = -Velocity.y; // Reverse velocity on collision
        }

        transform.position = position;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Particle")){
            ResolveParticleCollision(other.GetComponent<Particle>());
        }
    }

    void ResolveParticleCollision(Particle other)
    {
        Vector2 otherPosition = other.transform.position;
        Vector2 myPosition = transform.position;
        Vector2 collisionNormal = (myPosition - otherPosition).normalized;

        Vector2 relativeVelocity = Velocity - other.Velocity;
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, collisionNormal);

        if (velocityAlongNormal > 0)
            return;

        Velocity -= velocityAlongNormal*collisionNormal*Time.deltaTime;

        // Correct positions to avoid overlap
        float overlap = _radius + other._radius - Vector2.Distance(myPosition, otherPosition);
        Vector2 correction = collisionNormal * overlap;
        transform.position += (Vector3)correction;
    }


    // Draw gizmos to visualize the particle in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x / 2);
    }
}
