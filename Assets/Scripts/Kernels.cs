using UnityEngine;


/// <summary>
///  Kernel functions and their gradients
/// </summary>
public static class QuadraticKernel{
    public static float Calculate(float smoothingRadius, float distance)
    {
        return Mathf.Pow(1-distance/smoothingRadius, 2);
    }

    public static float Gradient(float smoothingRadius, float distance)
    {
        if (distance > smoothingRadius) return 0;
        return (float)(( 2*(smoothingRadius-distance) ) / Mathf.Pow(smoothingRadius, 2));
    }
}

public static class DebrunKernel{

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

public static class Poly6Kernel{

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

public static class ViscKernel{
    public static float Laplacian (float smoothingRadius, float distance)
    {
        if (distance > smoothingRadius) return 0;
        return ( 45/(Mathf.PI*Mathf.Pow(smoothingRadius, 6)) )* (smoothingRadius - distance);
    }
}