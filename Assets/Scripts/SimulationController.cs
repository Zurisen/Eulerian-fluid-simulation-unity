using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    // Boundary
    public Vector2 BoundarySize = new Vector2(20, 20);
    public Vector2 SpawnCenter = new Vector2(5,5);
    public float SpawnArea = 1f; 

    // External forces
    public float Gravity = -9.8f;


    // Fluid



    // Particles
    public int NumParticles = 50;
    /// GameObject to be assigned from editor
    [SerializeField]
    public GameObject ParticleGameObject;
    /// GameObject array to loop through the particles
    private GameObject[] _particleGameObject;
    private Vector2[] _particlePos;
    private Vector2[] _particleVel;


    void Awake()
    {
        _particleGameObject = new GameObject[NumParticles];
        _particlePos = new Vector2[NumParticles];
        _particleVel = new Vector2[NumParticles];
    }

    void Start(){
        SpawnParticles();
    }

    // Update is called once per frame
    void Update()
    {
        IntegrateParticles();
    }

    void IntegrateParticles(){
        for (int i = 0; i < NumParticles; i++){
            _particleVel[i].y += Gravity * Time.deltaTime;
            _particlePos[i] += _particleVel[i] * Time.deltaTime;
            HandleBoundaryCollisions(i);

            _particleGameObject[i].transform.position = _particlePos[i];
        }
    }

    void HandleBoundaryCollisions(int i)
    {
        if (_particlePos[i].x <= -BoundarySize.x / 2 || _particlePos[i].x >= BoundarySize.x / 2)
        {
            _particleVel[i].x = -0.2f*_particleVel[i].x;
            // Make sure the particle is within bounds after bounce
            _particlePos[i].x = Mathf.Clamp(_particlePos[i].x, -BoundarySize.x / 2, BoundarySize.x / 2);
        }
        if (_particlePos[i].y <= -BoundarySize.y / 2 || _particlePos[i].y >= BoundarySize.y / 2)
        {
            _particleVel[i].y = -0.2f*_particleVel[i].y;
            // Make sure the particle is within bounds after bounce
            _particlePos[i].y = Mathf.Clamp(_particlePos[i].y, -BoundarySize.y / 2, BoundarySize.y / 2);
        }
    }

    void SpawnParticles()
    {

        for (int i = 0; i < NumParticles; i++)
        {
            Vector2 spawnPosition = SpawnCenter + Random.insideUnitCircle*SpawnArea;
            GameObject particle = Instantiate(ParticleGameObject, spawnPosition, Quaternion.identity);
            _particleGameObject[i] = particle;
            _particlePos[i] = spawnPosition;
            _particleVel[i] = Vector2.zero;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector2.zero, BoundarySize);

        if (!Application.isPlaying){
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(SpawnCenter, SpawnArea);
        }

    }

}
