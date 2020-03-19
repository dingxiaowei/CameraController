using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Uti_Gizmos
{
    public static bool drawCircle(Vector3 origin, float radius, Quaternion rotation, int piece_count = 100)
    {
        if (3 > piece_count)
        {
            return false;
        }
        if (0 >= radius)
        {
            return false;
        }

        float piece_angle = 360.0f / piece_count;

        Vector3 p0 = origin + rotation * Vector3.forward * radius;
        Vector3 p1 = p0;
        for (int i = 0; i < piece_count - 1; ++i)
        {
            var r = Quaternion.Euler(0, piece_angle * (i + 1), 0);
            Vector3 p2 = origin + rotation * (r * Vector3.forward * radius);
            Gizmos.DrawLine(p1, p2);

            p1 = p2;
        }
        Gizmos.DrawLine(p0, p1);

        return true;
    }

    public static bool drawCircle(Vector3 origin, float radius, int pieceCount = 100)
    {
        return drawCircle(origin, radius, Quaternion.identity, pieceCount);
    }

    public static bool drawCylinder(Vector3 origin0, float radius, float high, Quaternion rotation, int piece_count = 100)
    {
        if (!drawCircle(origin0, radius, rotation, piece_count))
        {
            return false;
        }

        var origin1 = origin0 + rotation * Vector3.up * high;
        drawCircle(origin1, radius, rotation, piece_count);

        var p0_0 = origin0 + rotation * Vector3.forward * radius;
        var p0_1 = origin0 + rotation * Vector3.left * radius;
        var p0_2 = origin0 + rotation * Vector3.back * radius;
        var p0_3 = origin0 + rotation * Vector3.right * radius;

        var p1_0 = origin1 + rotation * Vector3.forward * radius;
        var p1_1 = origin1 + rotation * Vector3.left * radius;
        var p1_2 = origin1 + rotation * Vector3.back * radius;
        var p1_3 = origin1 + rotation * Vector3.right * radius;

        Gizmos.DrawLine(p0_0, p1_0);
        Gizmos.DrawLine(p0_1, p1_1);
        Gizmos.DrawLine(p0_2, p1_2);
        Gizmos.DrawLine(p0_3, p1_3);

        return true;
    }

    public static bool drawCylinder(Vector3 origin0, float radius, float high, int piece_count = 100)
    {
        return drawCylinder(origin0, radius, high, Quaternion.identity, piece_count);
    }
}