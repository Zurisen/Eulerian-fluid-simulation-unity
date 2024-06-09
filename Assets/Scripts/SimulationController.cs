using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public bool ChangeParticlesColor = false;
    public bool ShowSmoothingRadius = false;
    public float Stifness = 1f; // Ideal gas equation constant
    public float RestDensity = 0.001f; // helps with numerical stability
    public float TargetDensity = 0.6f;
    public float Gravity = -9.8f;
    public float PressureForceIntensity = 1f;
    public float SmoothingRadius = 3f;
    public Kernel DensityKernel;
    private Kernel ViscosityKernel = Kernel.Viscosity;

    private List<Particle> _particles;
    private SpatialHash _spatialHash;

    [SerializeField]
    private GameObject _fluidSpawner;

    private ParticleSpawner _particleSpawner;

    private List<Vector2> positions;
    private List<Vector2> velocities;

    private float deltaTime;

    void Awake()
    {
        if (_fluidSpawner == null)
        {
            throw new Exception("Particle Spawner Object needed to run simulation.");
        }

        _particleSpawner = _fluidSpawner.GetComponent<ParticleSpawner>();
        if (_particleSpawner == null)
        {
            throw new Exception("ParticleSpawner component not found on SpawnerObject.");
        }
        if (ShowSmoothingRadius) _particleSpawner.ParticlesAuraRadius = SmoothingRadius;

        _particles = _particleSpawner.GetParticles();
        _spatialHash = new SpatialHash(SmoothingRadius);

        // Initialize position and velocity lists
        positions = new List<Vector2>(_particles.Count);
        velocities = new List<Vector2>(_particles.Count);
        foreach (var particle in _particles)
        {
            positions.Add(particle.transform.position);
            velocities.Add(particle.Velocity);
        }
        deltaTime = Time.deltaTime;

    }

    void Update()
    {
        _spatialHash.Clear();

        for (int i = 0; i < _particles.Count; i++)
        {
            _spatialHash.Insert(i, positions[i]);
        }

        Parallel.For(0, _particles.Count, i =>
        {
            CalculateDensityAndPressure(i);
        });

        Parallel.For(0, _particles.Count, i =>
        {
            ApplyGravity(i);
            ApplyInteractions(i);
        });

        for (int i = 0; i < _particles.Count; i++)
        {
            UpdatePositionsAndVelocities(i);
        }

    }

    void ApplyGravity(int index)
    {
        velocities[index] += new Vector2(0, Gravity) * deltaTime;
    }

    void ApplyInteractions(int index)
    {
        Vector2 pressureForce = Vector2.zero;
        Vector2 viscosityForce = Vector2.zero;

        List<int> neighborIndices = _spatialHash.GetNeighbors(positions[index], SmoothingRadius);

        foreach (int neighborIndex in neighborIndices)
        {
            if (neighborIndex == index) continue;

            float distance = Vector2.Distance(positions[index], positions[neighborIndex]);
            Vector2 direction = (positions[index] - positions[neighborIndex]).normalized;
            float gradient = -KernelFunctionGradient(DensityKernel, SmoothingRadius + 0.0004f, distance + 0.0004f);
            float laplacian = -KernelFunctionLaplacian(ViscosityKernel, SmoothingRadius + 0.0004f, distance + 0.0004f);

            Particle neighbor = _particles[neighborIndex];
            pressureForce += neighbor.Mass * (neighbor.Pressure) / (2 * neighbor.Density) * gradient * direction;
            viscosityForce += Vector2.zero; //neighbor.Mass * (neighbor.Velocity - neighbor.Velocity) / neighbor.Density * laplacian;
        }
        float diffDensity = TargetDensity >= 0.05 ? Math.Abs(TargetDensity - _particles[index].Density) : 1;
        velocities[index] += (diffDensity * PressureForceIntensity * pressureForce + viscosityForce) / _particles[index].Density * deltaTime;

    }

    void CalculateDensityAndPressure(int index)
    {
        float density = RestDensity;
        List<int> neighborIndices = _spatialHash.GetNeighbors(positions[index], SmoothingRadius);

        foreach (int neighborIndex in neighborIndices)
        {
            if (neighborIndex == index) continue;
            float distance = Vector2.Distance(positions[index], positions[neighborIndex]);
            density += _particles[neighborIndex].Mass * KernelFunction(DensityKernel, SmoothingRadius + 0.0004f, distance + 0.0004f);
        }
        _particles[index].Density = density;
        _particles[index].Pressure = Stifness * density;
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
        switch (kernel)
        {
            case Kernel.Viscosity:
                return ViscKernel.Laplacian(smoothingRadius, distance);
            default:
                throw new ArgumentException("Not implemented Laplacian for this Density Kernel");
        }
    }

    void UpdatePositionsAndVelocities(int index)
    {
        if (ChangeParticlesColor) _particles[index].UpdateParticleColor();
        positions[index] += velocities[index] * deltaTime;
        ResolveBoundariesCollision(index);
        _particles[index].Velocity = velocities[index];
        _particles[index].transform.position = positions[index];
    }

    void ResolveBoundariesCollision(int index)
    {
        Vector2 particlePosition = positions[index];
        Vector2 velocity = velocities[index];
        float halfWidth = _particleSpawner.Width / 2f - _particles[index].transform.localScale.x / 2;
        float halfHeight = _particleSpawner.Height / 2f - _particles[index].transform.localScale.x / 2;
        Vector3 spawnerCenter = _particleSpawner.transform.position;

        if (particlePosition.x > spawnerCenter.x + halfWidth)
        {
            particlePosition.x = spawnerCenter.x + halfWidth;
            velocity.x = -velocity.x;
        }
        else if (particlePosition.x < spawnerCenter.x - halfWidth)
        {
            particlePosition.x = spawnerCenter.x - halfWidth;
            velocity.x = -velocity.x;
        }

        if (particlePosition.y > spawnerCenter.y + halfHeight)
        {
            particlePosition.y = spawnerCenter.y + halfHeight;
            velocity.y = -velocity.y;
        }
        else if (particlePosition.y < spawnerCenter.y - halfHeight)
        {
            particlePosition.y = spawnerCenter.y - halfHeight;
            velocity.y = -velocity.y;
        }

        positions[index] = particlePosition;
        velocities[index] = velocity;
    }
}
