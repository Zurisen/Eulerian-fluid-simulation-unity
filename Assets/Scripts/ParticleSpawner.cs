using System.Collections.Generic;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    public uint N = 10;
    public float ParticlesMass = 1f;
    public float ParticlesSize = 1f;
    public float SpawnRandomness = 0.1f;
    public float InitMargin = 0.5f;
    public float Width = 10f;
    public float Height = 10f;

    [SerializeField]
    private GameObject _particlePrefab;
    private List<Particle> _particles;

    void Awake()
    {
        _particles = new List<Particle>();
        SpawnParticles();
    }

    public List<Particle> GetParticles()
    {
        return _particles;
    }

    void SpawnParticles()
    {
        float particleDiameter = ParticlesSize + InitMargin;

        int maxColumns = Mathf.FloorToInt(Width / particleDiameter);
        int maxRows = Mathf.FloorToInt(Height / particleDiameter);

        int particlesSpawned = 0;
        System.Random random = new System.Random();

        Vector3 origin = transform.position - new Vector3(Width / 2, Height / 2, 0);

        for (uint i = 0; i < maxRows && particlesSpawned < N; i++)
        {
            for (uint j = 0; j < maxColumns && particlesSpawned < N; j++)
            {
                float x = origin.x + particleDiameter / 2 + j * particleDiameter;
                float y = origin.y + particleDiameter / 2 + i * particleDiameter;

                if (x + particleDiameter / 2 > origin.x + Width || y + particleDiameter / 2 > origin.y + Height)
                {
                    continue;
                }

                x += (float)(random.NextDouble() * 2 - 1) * SpawnRandomness;
                y += (float)(random.NextDouble() * 2 - 1) * SpawnRandomness;

                Vector3 position = new Vector3(x, y, 0);
                GameObject particleObject = Instantiate(_particlePrefab, position, Quaternion.identity);
                Particle particleScript = particleObject.GetComponent<Particle>();
                if (particleScript != null)
                {
                    particleScript.Mass = ParticlesMass;
                    particleScript.ChangeParticleSize(ParticlesSize);
                }
                _particles.Add(particleScript);
                particlesSpawned++;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(Width, Height, 0));
    }

}
