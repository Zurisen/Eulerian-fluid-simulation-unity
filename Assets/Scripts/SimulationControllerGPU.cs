using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SimulationControllerGPU : MonoBehaviour
{
    // Boundary
    public Vector2 BoundarySize = new Vector2(20, 20);
    // External forces
    public bool SideBlast = false;


    // Fluid
    public float CellSize;
    private int numCellsX;
    private int numCellsY;
    private float h;
    private int numCells;

    private CellType[] _cellType;
    private Vector2[] _cellVel;
    private Vector2[] _cellPrevVel;

    private float[] p;
    private float[] s;

    public int GSIters = 10;
    public float OverRelaxation = 1.8f;
    public bool VorticityConfinement = true;

    private Vector2 _lastMousePosition;

    public GameObject cellPrefab;
    private SpriteRenderer[] _cellRenderers;
    private float dt;

    // GPU
    public ComputeShader fluidComputeShader;
    private ComputeBuffer cellVelBuffer;
    private int kernelIdSolveIncompressibilityRed;
    private int kernelIdSolveIncompressibilityBlack;    
    private int kernelIdExtrapolateVelocities;
    private int kernelIdApplyAdvection;

    void Awake()
    {
        // Fluid
        numCellsX = (int)(Math.Floor(BoundarySize.x/CellSize)+1);
        numCellsY = (int)(Math.Floor(BoundarySize.y/CellSize)+1);
        h = Math.Max(BoundarySize.x/numCellsX, BoundarySize.y/numCellsY);
        numCells = numCellsX*numCellsY;
        _cellType = new CellType[numCells];
        _cellVel = new Vector2[numCells];
        _cellPrevVel = new Vector2[numCells];
        p = new float[numCells];
        s = new float[numCells];

        // Initialize cell objects and their renderers
        _cellRenderers = new SpriteRenderer[numCells];
        for (int i = 0; i < numCellsX; i++)
        {
            for (int j = 0; j < numCellsY; j++)
            {
                int cellIndex = getCellNrFromCoord(i, j);
                Vector3 cellPosition = new Vector3(
                    i * h - BoundarySize.x / 2 + h/2,
                    j * h - BoundarySize.y / 2 + h/2,
                    0);

                GameObject cell = Instantiate(cellPrefab, cellPosition, Quaternion.identity, transform);
                _cellRenderers[cellIndex] = cell.GetComponent<SpriteRenderer>();
                _cellRenderers[cellIndex].size = Vector2.one*h;
            }
        }


    }

    void InitializeComputeShader(){
        fluidComputeShader.SetFloat("deltaTime", dt);
        fluidComputeShader.SetInt("numCellsX", numCellsX);
        fluidComputeShader.SetInt("numCellsY", numCellsY);
        fluidComputeShader.SetFloat("h", h);
        fluidComputeShader.SetFloat("overRelaxation", OverRelaxation);
        fluidComputeShader.SetInt("vorticityConfinement", VorticityConfinement ? 1 : 0);

        cellVelBuffer = new ComputeBuffer(numCells, sizeof(float) * 2);

        // Solve incompressibility shader and buffers
        kernelIdSolveIncompressibilityRed = fluidComputeShader.FindKernel("SolveIncompressibilityRed");
        kernelIdSolveIncompressibilityBlack = fluidComputeShader.FindKernel("SolveIncompressibilityBlack");
        fluidComputeShader.SetBuffer(kernelIdSolveIncompressibilityRed, "cellVel", cellVelBuffer);
        fluidComputeShader.SetBuffer(kernelIdSolveIncompressibilityBlack, "cellVel", cellVelBuffer);
        
        // Apply advection shader and buffers
        kernelIdApplyAdvection = fluidComputeShader.FindKernel("ApplyAdvection");
        fluidComputeShader.SetBuffer(kernelIdApplyAdvection, "cellVel", cellVelBuffer);



    }

    void Start(){
        dt = Time.deltaTime;
        for (int i = 0; i < numCells; i++)
        {
            _cellType[i] = CellType.Fluid;
        }

        InitializeComputeShader();
    }

    void UpdateGrid()
    {
        for (int i = 0; i < numCells; i++)
        {
            int x = i / numCellsY;
            int y = i % numCellsY;

            if (x < 1 || y < 1 || x > numCellsX-2 || y > numCellsY-2){
                _cellVel[i] = Vector2.zero;
                var velMagnitude = _cellVel[i].magnitude;
                Color cellColor = Color.Lerp(Color.black, Color.cyan, velMagnitude / 10f);
                _cellRenderers[i].color = cellColor;
            } else {
                var velMagnitude = _cellVel[i].magnitude;
                Color cellColor = Color.Lerp(Color.black, Color.cyan, velMagnitude / 10f);
                _cellRenderers[i].color = cellColor;
            }
        }
    }



    void InitBlast(){
        int centerY = numCellsY/2;
        int fumeWidth = (int) (0.02f*numCellsY); 
        int i = 1;
        for (int j = centerY-fumeWidth; j < centerY+fumeWidth; j++)
        {
            var n = getCellNrFromCoord(i, j);
            _cellVel[n] += new Vector2(30,0);
        }

    }

    // Update is called once per frame
    void Update()
    {
        int threadGroupsX = Mathf.CeilToInt((float)numCellsX / 16);
        int threadGroupsY = Mathf.CeilToInt((float)numCellsY / 16);

        if (SideBlast) InitBlast();

        cellVelBuffer.SetData(_cellVel);
        // Perform Gauss-Seidel iterations
        for (int i = 0; i < GSIters; i++)
        {
            // Red pass
            fluidComputeShader.Dispatch(kernelIdSolveIncompressibilityRed, threadGroupsX, threadGroupsY, 1);
            // Black pass
            fluidComputeShader.Dispatch(kernelIdSolveIncompressibilityBlack, threadGroupsX, threadGroupsY, 1);
        }
        cellVelBuffer.GetData(_cellVel);

        ExtrapolateVelocities();

        cellVelBuffer.SetData(_cellVel);
        fluidComputeShader.Dispatch(kernelIdApplyAdvection, threadGroupsX, threadGroupsY, 1);
        cellVelBuffer.GetData(_cellVel);

        HandleMouseInput();
        UpdateGrid();
    }


    private int getCellNrFromCoord(int i, int j) {
        return Math.Clamp(i * numCellsY + j, 0, numCells - 1);
    }

    private bool checkIfCellIsValid(int xc, int yc) {
        if (xc < 0 || yc < 0 || xc >= numCellsX || yc >= numCellsY) return false;
        
        var cellNr = getCellNrFromCoord(xc, yc);
        if (_cellType[cellNr] != CellType.Fluid) return false;

        return true;
    }


    void SolveIncompressibility(int numIters, float overRelaxation)
    {
        int n = numCellsY;
        Array.Clear(p, 0, p.Length);
        Array.Copy(_cellVel, _cellPrevVel, _cellVel.Length);

        for (int iter = 0; iter < numIters; iter++)
        {
            Parallel.For(0, numCellsX * numCellsY, index =>
            {
                int i = index / numCellsY;
                int j = index % numCellsY;

                if (i == 0 || i == numCellsX - 1 || j == 0 || j == numCellsY - 1) return;
                if (!checkIfCellIsValid(i, j)) return;

                int left = i * n + j;
                int right = (i + 1) * n + j;
                int bottom = i * n + j;
                int top = i * n + (j + 1);

                var isValidLeft = checkIfCellIsValid(i, j);
                var isValidRight = checkIfCellIsValid(i + 1, j);
                var isValidTop = checkIfCellIsValid(i, j + 1);
                var isValidBot = checkIfCellIsValid(i, j);

                float div = (isValidRight ? _cellVel[right].x : 0f) -
                            (isValidLeft ? _cellVel[left].x : 0f) +
                            (isValidTop ? _cellVel[top].y : 0f) -
                            (isValidBot ? _cellVel[bottom].y : 0f);
                float denominator = new bool[] { isValidLeft, isValidRight, isValidTop, isValidBot }.Count(b => b);

                if (denominator > 0)
                {
                    float newPressure = -div / denominator;
                    newPressure *= overRelaxation;

                    if (isValidLeft) _cellVel[left].x -= newPressure;
                    if (isValidRight) _cellVel[right].x += newPressure;
                    if (isValidBot) _cellVel[bottom].y -= newPressure;
                    if (isValidTop) _cellVel[top].y += newPressure;
                }
            });
        }
    }

    void ExtrapolateVelocities()
    {
        var n = numCellsY;
        Parallel.For(0, numCellsX, i =>
        {
            _cellVel[i * n + 0].x = _cellVel[i * n + 1].x;
            _cellVel[i * n + numCellsY - 1].x = _cellVel[i * n + numCellsY - 2].x;
        });

        Parallel.For(0, numCellsY, j =>
        {
            _cellVel[0 * n + j].y = _cellVel[1 * n + j].y;
            _cellVel[(numCellsX - 1) * n + j].y = _cellVel[(numCellsX - 2) * n + j].y;
        });
    }


    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 mouseDelta = currentMousePosition - _lastMousePosition;

            if (mouseDelta.magnitude > 0)
            {
                Vector2 worldPosition = Camera.main.ScreenToWorldPoint(currentMousePosition);
                Vector2 lastWorldPosition = Camera.main.ScreenToWorldPoint(_lastMousePosition);
                Vector2 direction = (worldPosition - lastWorldPosition).normalized;
                float speed = mouseDelta.magnitude / dt;

                // Adjust the world position to match grid coordinates
                int cellX = Mathf.FloorToInt((worldPosition.x + BoundarySize.x / 2) / h);
                int cellY = Mathf.FloorToInt((worldPosition.y + BoundarySize.y / 2) / h);

                if (cellX >= 0 && cellX < numCellsX && cellY >= 0 && cellY < numCellsY && checkIfCellIsValid(cellX, cellY))
                {
                    int cellIndex = getCellNrFromCoord(cellX, cellY);
                    _cellVel[cellIndex] += new Vector2(direction.x, direction.y) * speed;
                }

                _lastMousePosition = currentMousePosition;
            }
        }
    }



    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(new Vector2(-2*h,-2*h), BoundarySize);
    }



}


