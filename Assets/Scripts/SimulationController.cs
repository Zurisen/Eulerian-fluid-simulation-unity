using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.UIElements;

public class SimulationController : MonoBehaviour
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

    void Start(){
        for (int i = 0; i < numCells; i++)
        {
            _cellType[i] = CellType.Fluid;
        }
    }

    void UpdateGrid(){
        for (int i = 0; i < numCells; i++)
        {
            var velMagnitude = _cellVel[i].magnitude;
            Color cellColor = Color.Lerp(Color.black, Color.cyan, velMagnitude / 10f);
            _cellRenderers[i].color = cellColor;
        }
    }


    void InitBlast(){
        int centerY = numCellsY/2;
        int fumeWidth = (int) (0.02f*numCellsY); 
        int i = 0;
        for (int j = centerY-fumeWidth; j < centerY+fumeWidth; j++)
        {
            var n = getCellNrFromCoord(i, j);
            _cellVel[n] += new Vector2(30,0);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (SideBlast) InitBlast();
        SolveIncompressibility(GSIters, OverRelaxation);

        ExtrapolateVelocities();
        ApplyAdvection();

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
        // Initialize pressure
        Array.Clear(p, 0, p.Length);

        // Save previous velocities
        Array.Copy(_cellVel, _cellPrevVel, _cellVel.Length);

        // Iterate to solve for pressure
        for (int iter = 0; iter < numIters; iter++)
        {
            for (int i = 1; i < numCellsX-1; i++)
            {
                for (int j = 1; j < numCellsY-1; j++)
                {

                    if (!checkIfCellIsValid(i, j)) continue;

                    int left = i * n + j;
                    int right = (i + 1) * n + j;
                    int bottom = i * n + j;
                    int top = i * n + (j + 1);

                    var isValidLeft = checkIfCellIsValid(i, j);
                    var isValidRight = checkIfCellIsValid(i+1, j);
                    var isValidTop = checkIfCellIsValid(i, j+1);
                    var isValidBot = checkIfCellIsValid(i, j);

                    float div = (isValidRight ? _cellVel[right].x : 0f) - 
                                (isValidLeft ? _cellVel[left].x : 0f) + 
                                (isValidTop ? _cellVel[top].y : 0f) - 
                                (isValidBot ? _cellVel[bottom].y : 0f);
                    float denominator = new bool[] { isValidLeft, isValidRight, isValidTop, isValidBot }.Count(b => b);

                    if (denominator > 0){
                        float newPressure = -div / denominator;

                        newPressure *= overRelaxation;

                        if (isValidLeft) _cellVel[left].x -= newPressure;
                        if (isValidRight) _cellVel[right].x += newPressure;
                        if (isValidBot) _cellVel[bottom].y -= newPressure;
                        if (isValidTop) _cellVel[top].y += newPressure;
                    }

                }
            }
        }

    }

    void ExtrapolateVelocities(){
        var n = numCellsY;
        for (int i = 0; i < numCellsX; i++)
        {
            _cellVel[i*n + 0].x = _cellVel[i*n +1].x;
            _cellVel[i*n + numCellsY-1].x = _cellVel[i*n + numCellsY-2].x; 
        }

        for (int j = 0; j < numCellsY; j++)
        {
            _cellVel[0*n + j].y = _cellVel[1*n + j].y;
            _cellVel[(numCellsX-1)*n + j].y = _cellVel[(numCellsX-2)*n + j].y;
        }
    }

    void ApplyAdvection(){

        Vector2[] newVel = new Vector2[numCells];

        for (int i = 1; i < numCellsX-1; i++)
        {
            for (int j = 1; j < numCellsY-1; j++)
            {
                if (!checkIfCellIsValid(i,j)) continue;
                
                var n = getCellNrFromCoord(i,j);
                
                // We compute the previous location of the semi-lagrangian particle that 
                // would arrive at the position where the velocities are located 
                if (VorticityConfinement){
                    var avg = AverageVector(i, j);
                    
                    //// for the x component of the velocity, located at (i*h, j*h+h/2)
                    var x_u = i*h -_cellVel[n].x*Time.deltaTime;
                    var y_u = j*h+h/2 -avg.y*Time.deltaTime;
                    //// now that we now the position, we interpolate the velocity (since this position doesn't correspond to the
                    /// staggered grid velocities positions)
                    var u = InterpolateField(x_u, y_u, _cellVel, component: 0);


                    //// for the y component of the velocity, located at (i*h, j*h+h/2)
                    var x_v = i*h+h/2 -avg.x*Time.deltaTime;
                    var y_v = j*h - _cellVel[n].y*Time.deltaTime;
                    //// now that we now the position, we interpolate the velocity (since this position doesn't correspond to the
                    /// staggered grid velocities positions)
                    var v = InterpolateField(x_v, y_v, _cellVel, component: 1);

                    newVel[n] = new Vector2(u, v);
                }



            }
            
        }

        // Now we move the semilagrangian particle "previous" velocity to the current cell velocity
        Array.Copy(newVel, _cellVel, numCells);

    }

    Vector2 AverageVector(int i, int j){
        // Since we have a staggered grid we just average over the 4 surrounding points 

        // for the y component
        int topleft = getCellNrFromCoord(i-1, j+1);
        int topright = getCellNrFromCoord(i, j+1);
        int botleft = getCellNrFromCoord(i-1, j);
        int botright = getCellNrFromCoord(i, j);
        var isValidTopLeft = checkIfCellIsValid(i-1, j+1);
        var isValidTopRight = checkIfCellIsValid(i, j+1);
        var isValidBotLeft = checkIfCellIsValid(i-1, j);
        var isValidBotRight = checkIfCellIsValid(i, j);

        float avgY = (isValidTopLeft ? _cellVel[topleft].y : 0f) +
            (isValidTopRight ? _cellVel[topright].y : 0f) + 
            (isValidBotLeft ? _cellVel[botleft].y : 0f) + 
            (isValidBotRight ? _cellVel[botright].y : 0f);
        avgY /= 4;

        // for the x component
        topleft = getCellNrFromCoord(i, j);
        topright = getCellNrFromCoord(i+1, j);
        botleft = getCellNrFromCoord(i, j-1);
        botright = getCellNrFromCoord(i+1, j-1);
        isValidTopLeft = checkIfCellIsValid(i-1, j+1);
        isValidTopRight = checkIfCellIsValid(i, j+1);
        isValidBotLeft = checkIfCellIsValid(i-1, j);
        isValidBotRight = checkIfCellIsValid(i, j);

        float avgX = (isValidTopLeft ? _cellVel[topleft].y : 0f) +
            (isValidTopRight ? _cellVel[topright].y : 0f) + 
            (isValidBotLeft ? _cellVel[botleft].y : 0f) + 
            (isValidBotRight ? _cellVel[botright].y : 0f);
        avgX /= 4;

        return new Vector2(avgX, avgY);
    }

    float InterpolateField(float x, float y, Vector2[] field, int component){
        if (component > 1 || component < 0) throw new Exception("Wrong component to interpolate field");

        float h2 = h/2;
        
        float x0 = (float)Math.Min(Math.Floor((x-h2)/h), numCellsX-1);
        float tx = ((x-h2) - x0*h)/h;
        float x1 = Math.Min(x0+1, numCellsX-1);

        float y0 = (float)Math.Min(Math.Floor((y-h2)/h), numCellsY-1);
        float ty = ((y-h2) - y0*h)/h;
        float y1 = Math.Min(y0+1, numCellsX-1);
    
        float sx = 1.0f - tx;
        float sy = 1.0f - ty;

        int n = numCellsY;
        int indx0 = getCellNrFromCoord((int)x0, (int)y0);
        int indx1 = getCellNrFromCoord((int)x1, (int)y0);
        int indx2 = getCellNrFromCoord((int)x1, (int)y1);
        int indx3 = getCellNrFromCoord((int)x0, (int)y1);

        float val = 0.0f;

        if (component == 0) {
            val = sx*sy * field[indx0].x +
                tx*sy * field[indx1].x +
                tx*ty * field[indx2].x +
                sx*ty * field[indx3].x;
        } else if (component == 1){
            val = sx*sy * field[indx0].y +
                tx*sy * field[indx1].y +
                tx*ty * field[indx2].y +
                sx*ty * field[indx3].y;
        }

        return val;
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
            float speed = mouseDelta.magnitude / Time.deltaTime;

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


    void DrawArrow(Vector3 start, Vector3 end) {
        Gizmos.DrawLine(start, end);
        
        Vector3 direction = (end - start).normalized;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);

        Gizmos.DrawLine(end, end + right * 0.1f);
        Gizmos.DrawLine(end, end + left * 0.1f);
    }



}

public enum CellType{
    Solid,
    Fluid,
}

