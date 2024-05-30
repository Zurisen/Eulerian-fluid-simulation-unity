using System;
using System.Collections.Generic;
using UnityEngine;

public enum DensityKernel
{
    Quadratic,
    Poly6,
    Debrun
}



public class SimulationController : MonoBehaviour
{
    public uint N = 10;
    public float ParticlesSize = 1f;
    public float ParticlesMass = 1f;
    public float Stifness = 1f;
    public float RestDensity = 0.1f;
    public float InitMargin = 0.5f;
    public float Gravity = -9.8f;
    private Vector2 _gravity = Vector2.zero;

    public float MainSmoothingRadius = 3f;
    public DensityKernel MainDensityKernel;
    public float LeftBoundary = -10;
    public float RightBoundary = 10;
    public float TopBoundary = 10;
    public float BottomBoundary = -10;


    [SerializeField]
    private GameObject _particlePrefab;

    private List<Particle> _particles;


    void Awake(){
        _gravity = new Vector2(0, Gravity * Time.deltaTime);
        _particles = new List<Particle>();
        SpawnParticles();
    }


    // Update is called once per frame
    void Update()
    {
        foreach (Particle particle in _particles)
        {
            CalculateDensityAndPressure(particle);

        }

        foreach (Particle particle in _particles)
        {
            ApplyGravity(particle);
            ApplyPressure(particle);
            UpdatePosition(particle);

        }
    }

    void ApplyGravity(Particle particle)
    {
        particle.Velocity += _gravity * Time.deltaTime;
    }

    void ApplyPressure(Particle particle)
    {
        Vector2 pressureForce = Vector2.zero;
        foreach (Particle neighbour in _particles)
        {
            if (particle == neighbour) continue;
            
            float distance = Vector2.Distance(particle.transform.position, neighbour.transform.position);
            Vector2 direction = (particle.transform.position - neighbour.transform.position).normalized;
            float gradient = KernelFunctionGradient(MainSmoothingRadius+0.0004f, distance+0.0004f);
            pressureForce += -particle.Mass * (particle.Pressure + neighbour.Pressure) / (2 * neighbour.Density) * gradient * direction;
        }
        particle.Velocity += (pressureForce/particle.Density) * Time.deltaTime;
    }

    float KernelFunctionGradient(float smoothingRadius, float distance){
        switch (MainDensityKernel)
        {
            case DensityKernel.Quadratic:
                return QuadraticDensityKernel.Calculate(smoothingRadius, distance);  
            case DensityKernel.Poly6:
                return Poly6DensityKernel.Calculate(smoothingRadius, distance);
            case DensityKernel.Debrun:
                return DebrunDensityKernel.Calculate(smoothingRadius, distance);
            default:
                throw new ArgumentException("Not implemented Density Kernel");
        }
    }

    void CalculateDensityAndPressure(Particle particle)
    {
        float density = RestDensity;
        foreach (Particle neighbour in _particles)
        {
            if (neighbour == particle) continue;
            float distance = Vector2.Distance(particle.transform.position, neighbour.transform.position);
            density += neighbour.Mass * (-1) * KernelFunction(MainSmoothingRadius+0.0004f, distance+0.0004f); // -1 because they attract each other otherwise
        } 
        particle.Density = density;
        particle.Pressure = Stifness * density;
    }

    float KernelFunction(float smoothingRadius, float distance)
    {
        switch (MainDensityKernel)
        {
            case DensityKernel.Quadratic:
                return QuadraticDensityKernel.Calculate(smoothingRadius, distance);  
            case DensityKernel.Poly6:
                return Poly6DensityKernel.Calculate(smoothingRadius, distance);
            case DensityKernel.Debrun:
                return DebrunDensityKernel.Calculate(smoothingRadius, distance);
            default:
                throw new ArgumentException("Not implemented Density Kernel");
        }

    }




    void UpdatePosition(Particle particle)
    {

        particle.transform.Translate(particle.Velocity*Time.deltaTime);
        ResolveBoundariesCollision(particle);
    }


    void ResolveBoundariesCollision(Particle particle){

        Vector2 particlePosition = particle.transform.position;
        if (particlePosition.x > RightBoundary)
        {
            particlePosition.x = RightBoundary;
            particle.Velocity.x = -particle.Velocity.x; // Reverse velocity on collision
        }
        else if (particlePosition.x < LeftBoundary)
        {
            particlePosition.x = LeftBoundary;
            particle.Velocity.x = -particle.Velocity.x; // Reverse velocity on collision
        }

        if (particlePosition.y > TopBoundary)
        {
            particlePosition.y = TopBoundary;
            particle.Velocity.y = -particle.Velocity.y; // Reverse velocity on collision
        }
        else if (particlePosition.y < BottomBoundary)
        {
            particlePosition.y = BottomBoundary;
            particle.Velocity.y = -particle.Velocity.y; // Reverse velocity on collision
        }

        particle.transform.position = particlePosition;
    }

    void SpawnParticles()
    {
        float areaWidth = RightBoundary - LeftBoundary;
        float areaHeight = TopBoundary - BottomBoundary;
        float particleDiameter = ParticlesSize + InitMargin;

        int maxColumns = Mathf.FloorToInt(areaWidth / particleDiameter);
        int maxRows = Mathf.FloorToInt(areaHeight / particleDiameter);

        int particlesSpawned = 0;
        for (uint i = 0; i < maxRows && particlesSpawned < N; i++)
        {
            for (uint j = 0; j < maxColumns && particlesSpawned < N; j++)
            {
                float x = LeftBoundary+ particleDiameter / 2 + j * particleDiameter;
                float y = BottomBoundary+ particleDiameter / 2 + i * particleDiameter;

                if (x + particleDiameter / 2 > RightBoundary || y + particleDiameter / 2 > TopBoundary)
                {
                    continue;
                }

                Vector3 position = new Vector3(x, y, 0);
                GameObject particleObject = Instantiate(_particlePrefab, position, Quaternion.identity);
                Particle particleScript = particleObject.GetComponent<Particle>();
                if (particleObject != null)
                {
                    particleScript.ChangeParticleSize(ParticlesSize);
                }
                _particles.Add(particleScript);
                particlesSpawned++;
            }
        }
    }

        // Draw gizmos to visualize the simulation area and particles in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(LeftBoundary, TopBoundary, 0), new Vector3(RightBoundary, TopBoundary, 0));
        Gizmos.DrawLine(new Vector3(RightBoundary, TopBoundary, 0), new Vector3(RightBoundary, BottomBoundary, 0));
        Gizmos.DrawLine(new Vector3(RightBoundary, BottomBoundary, 0), new Vector3(LeftBoundary, BottomBoundary, 0));
        Gizmos.DrawLine(new Vector3(LeftBoundary, BottomBoundary, 0), new Vector3(LeftBoundary, TopBoundary, 0));
        if (_particles == null)
        {
            float areaWidth = RightBoundary - LeftBoundary;
            float areaHeight = TopBoundary - BottomBoundary;
            float particleDiameter = ParticlesSize + InitMargin;

            int maxColumns = Mathf.FloorToInt(areaWidth / particleDiameter);
            int maxRows = Mathf.FloorToInt(areaHeight / particleDiameter);

            int particlesSpawned = 0;
            for (int i = 0; i < maxRows && particlesSpawned < N; i++)
            {
                for (int j = 0; j < maxColumns && particlesSpawned < N; j++)
                {
                    float x = LeftBoundary + particleDiameter / 2 + j * particleDiameter;
                    float y = BottomBoundary + particleDiameter / 2 + i * particleDiameter;

                    if (x + particleDiameter / 2 > RightBoundary || y + particleDiameter / 2 > TopBoundary)
                    {
                        continue;
                    }

                    Vector3 position = new Vector3(x, y, 0);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(position, ParticlesSize / 2);
                    particlesSpawned++;
                }
            }
        }
    }
}




/// <summary>
///  Kernel functions and their gradients
/// </summary>
public static class QuadraticDensityKernel{
    public static float Calculate(float smoothingRadius, float distance)
    {
        return Mathf.Pow(1-distance/smoothingRadius, 2);
    }

    public static float Gradient(float smoothingRadius, float distance)
    {
        return (float)(( -2*(smoothingRadius-distance) ) / Math.Pow(smoothingRadius, 2));
    }
}

public static class DebrunDensityKernel{

    public static float Calculate(float smoothingRadius, float distance)
    {
        if (distance > smoothingRadius) return 0;
        return ( 15/(Mathf.PI*Mathf.Pow(smoothingRadius,6)) )* Mathf.Pow(smoothingRadius-distance, 3);
    }

    public static float Gradient(float smoothingRadius, float distance){
        if (distance > smoothingRadius) return 0;
        return ( -45/(Mathf.PI*Mathf.Pow(smoothingRadius,6)) )*Mathf.Pow(smoothingRadius-distance, 2);
    }

}

public static class Poly6DensityKernel{

    public static float Calculate(float smoothingRadius, float distance)
    {
        if (distance > smoothingRadius) return 0;
        return ( 315/(64*Mathf.PI*Mathf.Pow(smoothingRadius,9)) )* Mathf.Pow(Mathf.Pow(smoothingRadius,2)-Mathf.Pow(distance,2), 3);
    }
    public static float Gradient(float smoothingRadius, float distance){
        if (distance > smoothingRadius) return 0;
        return ( -945*distance/(32*Mathf.PI*Mathf.Pow(smoothingRadius,9)) )*Mathf.Pow(Mathf.Pow(smoothingRadius,2)-Mathf.Pow(distance,2), 2);
    }

}    