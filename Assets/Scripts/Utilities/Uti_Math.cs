using System;
using UnityEngine;

public static class Uti_Math {

    public static bool equalsZero(this float v)
    {
        return Math.Abs(v) < float.Epsilon;
    }

    public static float uniformAngle360(this float angle)
    {
        return angle - Mathf.Floor(angle / 360) * 360;
    }

}
