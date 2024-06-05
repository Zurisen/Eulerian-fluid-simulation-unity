using System;
using System.Collections.Generic;
using UnityEngine;

public enum Kernel
{
    Quadratic,
    Poly6,
    Debrun,
    Viscosity
}

public class SimulationController : MonoBehaviour
{
    public float Stifness = 1f; // Ideal gas equation constant
    public float RestDensity = 0.001f; // helps with numerical stability
    public float Gravity = -9.8f;
    public float PressureForceIntensity = 1f;
    private Vector2 _gravity = Vector2.zero;
    public float SmoothingRadius = 3f;
    public Kernel DensityKernel;
    private Kernel ViscosityKernel = Kernel.Viscosity;

    private List<Particle> _particles;
    private SpatialHash _spatialHash;

    [SerializeField]
    private GameObject _fluidSpawner;

    private ParticleSpawner _particleSpawner;

    void Awake()
    {
        _gravity = new Vector2(0, Gravity * Time.deltaTime);
        
        if (_fluidSpawner == null)
        {
            throw new Exception("Particle Spawner Object needed to run simulation.");
        }

        _particleSpawner = _fluidSpawner.GetComponent<ParticleSpawner>();
        if (_particleSpawner == null)
        {
            throw new Exception("ParticleSpawner component not found on SpawnerObject.");
        }

        _particles = _particleSpawner.GetParticles();
        _spatialHash = new SpatialHash(SmoothingRadius);
    }

    void Update()
    {
        _spatialHash.Clear();

        foreach (Particle particle in _particles)
        {
            _spatialHash.Insert(particle);
        }

        foreach (Particle particle in _particles)
        {
            CalculateDensityAndPressure(particle);
        }

        foreach (Particle particle in _particles)
        {
            ApplyGravity(particle);
            ApplyInteractions(particle);
            UpdatePosition(particle);

        }
    }

    void ApplyGravity(Particle particle)
    {
        particle.Velocity += _gravity * Time.deltaTime;
    }

    void ApplyInteractions(Particle particle)
    {
        Vector2 pressureForce = Vector2.zero;
        Vector2 viscosityForce = Vector2.zero;

        List<Particle> neighbors = _spatialHash.GetNeighbors(particle.transform.position, SmoothingRadius);

        foreach (Particle neighbour in neighbors)
        {
            if (particle == neighbour) continue;
            
            float distance = Vector2.Distance(particle.transform.position, neighbour.transform.position);
            Vector2 direction = (particle.transform.position - neighbour.transform.position).normalized;
            float gradient = -KernelFunctionGradient(DensityKernel, SmoothingRadius+0.0004f, distance+0.0004f);
            float laplacian = -KernelFunctionLaplacian(ViscosityKernel, SmoothingRadius+0.0004f, distance+0.0004f);

            pressureForce += neighbour.Mass * (particle.Pressure + neighbour.Pressure) / (2 * neighbour.Density) * gradient * direction;
            viscosityForce += Vector2.zero; //neighbour.Mass * (neighbour.Velocity - neighbour.Velocity) / neighbour.Density * laplacian;
        }
        particle.Velocity += (PressureForceIntensity* pressureForce + viscosityForce) /particle.Density * Time.deltaTime;
    }

    void CalculateDensityAndPressure(Particle particle)
    {
        float density = RestDensity;
        List<Particle> neighbors = _spatialHash.GetNeighbors(particle.transform.position, SmoothingRadius);

        foreach (Particle neighbour in neighbors)
        {
            if (neighbour == particle) continue;
            float distance = Vector2.Distance(particle.transform.position, neighbour.transform.position);
            density += neighbour.Mass * KernelFunction(DensityKernel, SmoothingRadius+0.0004f, distance+0.0004f);
        } 
        particle.Density = density;
        particle.Pressure = Stifness * density;
    }

    float KernelFunction(Kernel kernel, float smoothingRadius, float distance)
    {
        switch (DensityKernel)
        {
            case Kernel.Quadratic:
                return QuadraticKernel.Calculate(smoothingRadius, distance);  
            case Kernel.Poly6:
                return Poly6Kernel.Calculate(smoothingRadius, distance);
            case Kernel.Debrun:
                return DebrunKernel.Calculate(smoothingRadius, distance);
            default:
                throw new ArgumentException("Not implemented Density Kernel");
        }
    }

    float KernelFunctionGradient(Kernel kernel, float smoothingRadius, float distance)
    {
        switch (kernel)
        {
            case Kernel.Quadratic:
                return QuadraticKernel.Gradient(smoothingRadius, distance);  
            case Kernel.Poly6:
                return Poly6Kernel.Gradient(smoothingRadius, distance);
            case Kernel.Debrun:
                return DebrunKernel.Gradient(smoothingRadius, distance);
            default:
                throw new ArgumentException("Not implemented Gradient for this Density Kernel");
        }
    }

    float KernelFunctionLaplacian(Kernel kernel, float smoothingRadius, float distance)
    {
        switch(kernel)
        {
            case Kernel.Viscosity:
                return ViscKernel.Laplacian(smoothingRadius, distance);   
            default:
                throw new ArgumentException("Not implemented Laplacian for this Density Kernel");
        }
    }

    void UpdatePosition(Particle particle)
    {
        particle.UpdateParticleColor();
        particle.transform.Translate(particle.Velocity * Time.deltaTime);
        ResolveBoundariesCollision(particle);
    }

    void ResolveBoundariesCollision(Particle particle)
    {
        Vector3 particlePosition = particle.transform.position;
        float halfWidth = _particleSpawner.Width / 2f - particle.transform.localScale.x/2;
        float halfHeight = _particleSpawner.Height / 2f - particle.transform.localScale.x/2;
        Vector3 spawnerCenter = _particleSpawner.transform.position;

        if (particlePosition.x > spawnerCenter.x + halfWidth)
        {
            particlePosition.x = spawnerCenter.x + halfWidth;
            particle.Velocity.x = -particle.Velocity.x;
        }
        else if (particlePosition.x < spawnerCenter.x - halfWidth)
        {
            particlePosition.x = spawnerCenter.x - halfWidth;
            particle.Velocity.x = -particle.Velocity.x;
        }

        if (particlePosition.y > spawnerCenter.y + halfHeight)
        {
            particlePosition.y = spawnerCenter.y + halfHeight;
            particle.Velocity.y = -particle.Velocity.y;
        }
        else if (particlePosition.y < spawnerCenter.y - halfHeight)
        {
            particlePosition.y = spawnerCenter.y - halfHeight;
            particle.Velocity.y = -particle.Velocity.y;
        }

        particle.transform.position = particlePosition;
    }
}
