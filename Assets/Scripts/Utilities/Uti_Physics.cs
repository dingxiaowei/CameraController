using System;
using System.Collections.Generic;
using UnityEngine;

public static class Uti_Physics
{
    public const float COLLISION_ACCURACY = 0.05f;

    public static bool physicsEqualsZero(this float v)
    {
        return Math.Abs(v) < 0.00001;
    }

    public static bool sphereCast(Vector3 origin, float radius, Vector3 dir, LayerMask obstacle_layers, out RaycastHit hit, out Vector3 hit_point, float max_distance = float.MaxValue)
    {
        if (!Physics.SphereCast(origin, radius, dir, out hit, max_distance, obstacle_layers))
        {
            hit_point = Vector3.zero;
            return false;
        }

        hit_point = origin + dir.normalized * hit.distance;
        return true;
    }

}
