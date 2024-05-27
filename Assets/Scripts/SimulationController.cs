using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public uint N = 10;
    public float Size = 1;
    public float InitMargin = 0.5f;
    public float Gravity = -9.8f;
    public float LeftBoundary = -10;
    public float RightBoundary = 10;
    public float TopBoundary = 10;
    public float BottomBoundary = -10;

    //Physics
    public float CollisionEllasticity = 1f;

    [SerializeField]
    private GameObject _particlePrefab;

    private List<GameObject> _particles;

    // Start is called before the first frame update
    void Start()
    {
        _particles = new List<GameObject>();
        SpawnParticles();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnParticles()
    {
        float areaWidth = RightBoundary - LeftBoundary;
        float areaHeight = TopBoundary - BottomBoundary;
        float particleDiameter = Size + InitMargin;

        int maxColumns = Mathf.FloorToInt(areaWidth / particleDiameter);
        int maxRows = Mathf.FloorToInt(areaHeight / particleDiameter);

        int particlesSpawned = 0;
        for (uint i = 0; i < maxRows && particlesSpawned < N; i++)
        {
            for (uint j = 0; j < maxColumns && particlesSpawned < N; j++)
            {
                float x = LeftBoundary + particleDiameter / 2 + j * particleDiameter;
                float y = BottomBoundary + particleDiameter / 2 + i * particleDiameter;

                if (x + particleDiameter / 2 > RightBoundary || y + particleDiameter / 2 > TopBoundary)
                {
                    continue;
                }

                Vector3 position = new Vector3(x, y, 0);
                GameObject particleObject = Instantiate(_particlePrefab, position, Quaternion.identity);
                Particle particleScript = particleObject.GetComponent<Particle>();
                if (particleObject != null)
                {
                    particleScript.ChangeParticleSize(Size);
                }
                _particles.Add(particleObject);
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
            float particleDiameter = Size + InitMargin;

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
                    Gizmos.DrawWireSphere(position, Size / 2);
                    particlesSpawned++;
                }
            }
        }
    }
}
