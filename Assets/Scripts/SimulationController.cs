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
    public Vector2 SpawnCenter = new Vector2(5,5);
    public float SpawnArea = 1f; 

    // External forces
    public float Gravity = -9.8f;


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

    public float Density = 1f;
    public int GSIters = 10;
    public float OverRelaxation = 1.8f;
    public bool VorticityConfinement = true;
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


    }

    void Start(){
        for (int i = 0; i < numCells; i++)
        {
            _cellType[i] = CellType.Fluid;
        }
        InitFume();
    }

    void InitFume(){
        int centerY = numCellsY/2;
        // int fumeWidth = (int) (0.1f*numCellsY); 
        int fumeWidth = 1;
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
        InitFume();
        ApplyGravity();
        SolveIncompressibility(GSIters, OverRelaxation);

        ExtrapolateVelocities();
        ApplyAdvection();
    }

    void ApplyGravity(){
        var n = numCellsY;
        for (int i = 0; i < numCellsX; i++)
        {
            for (int j = 0; j < numCellsY; j++)
            {
                // if it is solid or the last cell above the ground, we donot update the velocity
                var isValid = checkIfCellIsValid(i, j);
                var isBottomValid = checkIfCellIsValid(i, j-1);

                if (isValid && isBottomValid){
                    _cellVel[i*n + j] += Vector2.up*Gravity*Time.deltaTime;  
                }
            }
        }
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
        float cp = Density * h / Time.deltaTime;  // Assuming density is 1 for simplicity, adjust if needed
        // Initialize pressure
        Array.Clear(p, 0, p.Length);

        // Save previous velocities
        Array.Copy(_cellVel, _cellPrevVel, _cellVel.Length);

        // Iterate to solve for pressure
        for (int iter = 0; iter < numIters; iter++)
        {
            for (int i = 0; i < numCellsX; i++)
            {
                for (int j = 0; j < numCellsY; j++)
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
                        p[left] += cp * newPressure;

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

        for (int i = 0; i < numCellsX; i++)
        {
            for (int j = 0; j < numCellsY; j++)
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




    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector2.zero, BoundarySize);


        // Draw grid cells
        for (int i = 0; i < numCellsX; i++) {
            for (int j = 0; j < numCellsY; j++) {
                int cellIndex = i * numCellsY + j;

                // Calculate cell position
                float xPos = i * h - BoundarySize.x / 2 + h / 2;
                float yPos = j * h - BoundarySize.y / 2 + h / 2;
                Vector2 cellPos = new Vector2(xPos, yPos);

                // Calculate color based on velocity magnitude
                float velocityMagnitude = _cellVel[cellIndex].magnitude;
                Color cellColor = Color.Lerp(Color.blue, Color.white, velocityMagnitude / 50f);
                Gizmos.color = cellColor.WithAlpha(0.4f);
                // Draw the cell
                Gizmos.DrawCube(cellPos, new Vector3(h, h, 0));
                
                // Draw velocity text
                //Handles.color = Color.white;
                GUIStyle style = new GUIStyle();
                style.fontSize = 8;
                style.normal.textColor = Color.white;
                //Handles.Label(cellPos+new Vector2(0, h/2), ((int)velocityMagnitude).ToString(), style);

                // Draw velocity vector
                // Vector2 velocity = _cellVel[cellIndex].normalized/3;
                // Vector3 endPos = new Vector3(cellPos.x + velocity.x, cellPos.y + velocity.y, 0);
                // Gizmos.color = Color.white;
                // DrawArrow(new Vector3(cellPos.x, cellPos.y, 0), endPos);

            }

        }   
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

