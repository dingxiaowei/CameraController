using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Uti_Vector
{
    public static bool equalsZero(this Vector2 v)
    {
        return v.x.equalsZero() && v.y.equalsZero();
    }

    public static bool equalsZero(this Vector3 v)
    {
        return v.x.equalsZero() && v.y.equalsZero() && v.z.equalsZero();
    }

    public static Vector3 toVector3XZ(this Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }

    public static Vector2 toVector2XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static float calcYaw(this Vector2 dir_xz)
    {
        return Mathf.Atan2(dir_xz.x, dir_xz.y) * Mathf.Rad2Deg;
    }

    public static Vector3 rotateByAngles(this Vector3 vec, Vector3 angles)
    {
        return Quaternion.Euler(angles) * vec;
    }

    public static Vector3 rotateByAngleAxis(this Vector3 vec, float angle, Vector3 axis)
    {
        return Quaternion.AngleAxis(angle, axis) * vec;
    }
}
