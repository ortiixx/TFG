using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math = System.Math;

public class MathHelpers : MonoBehaviour
{
    public static Vector2 ClosestPointEllipse(Vector2 point, double semiMajor, double semiMinor) //Returns closest point from ellipse defined as ('semiMajor', 'semiMinor') from 'point'
    {
        double px = Mathf.Abs(point.x);
        double py = Mathf.Abs(point.y);

        double a = semiMajor;
        double b = semiMinor;

        double tx = 0.70710678118;
        double ty = 0.70710678118;

        double x, y, ex, ey, rx, ry, qx, qy, r, q, t = 0;

        for (int i = 0; i < 3; ++i)
        {
            x = a * tx;
            y = b * ty;

            ex = (a * a - b * b) * (tx * tx * tx) / a;
            ey = (b * b - a * a) * (ty * ty * ty) / b;

            rx = x - ex;
            ry = y - ey;

            qx = px - ex;
            qy = py - ey;

            r = Math.Sqrt(rx * rx + ry * ry);
            q = Math.Sqrt(qy * qy + qx * qx);

            tx = Math.Min(1, Math.Max(0, (qx * r / q + ex) / a));
            ty = Math.Min(1, Math.Max(0, (qy * r / q + ey) / b));

            t = Math.Sqrt(tx * tx + ty * ty);

            tx /= t;
            ty /= t;
        }

        return new Vector2
        {
            x = (float)(a * (point.x < 0 ? -tx : tx)),
            y = (float)(b * (point.y < 0 ? -ty : ty))
        };
    }

    public static void Decompose(Quaternion quaternion, Vector3 direction, out Quaternion swing, out Quaternion twist)
    {
        Vector3 vector = new Vector3(quaternion.x, quaternion.y, quaternion.z);
        Vector3 projection = Vector3.Project(vector, direction);

        twist = new Quaternion(projection.x, projection.y, projection.z, quaternion.w).normalized;
        swing = quaternion * Quaternion.Inverse(twist);
    }

    public static Quaternion Constrain(Quaternion quaternion, float angle)
    {
        float magnitude = Mathf.Sin(0.5F * angle);
        float sqrMagnitude = magnitude * magnitude;

        Vector3 vector = new Vector3(quaternion.x, quaternion.y, quaternion.z);

        if (vector.sqrMagnitude > sqrMagnitude)
        {
            vector = vector.normalized * magnitude;

            quaternion.x = vector.x;
            quaternion.y = vector.y;
            quaternion.z = vector.z;
            quaternion.w = Mathf.Sqrt(1.0F - sqrMagnitude) * Mathf.Sign(quaternion.w);
        }

        return quaternion;
    }

    public static void Quat2VecAngle(Quaternion quat, out Vector3 dir, out float angle)
    {
        angle = Mathf.Acos(quat.w)*2;
        float s = Mathf.Sin(angle * 0.5f);
        dir.x = quat.x / s;
        dir.y = quat.y / s;
        dir.z = quat.z / s;
        angle *= Mathf.Rad2Deg;
    }

    public static bool CheckPointInQuad(Vector3 bp1, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float f1 = Mathf.Sign(Vector3.Cross(bp1 - p1, p2 - p1).y);
        float f2 = Mathf.Sign(Vector3.Cross(bp1 - p2, p3 - p2).y);
        float f3 = Mathf.Sign(Vector3.Cross(bp1 - p3, p4 - p3).y);
        float f4 = Mathf.Sign(Vector3.Cross(bp1 - p4, p1 - p4).y);
        return f1 == f2 && f3 == f4 && f4 == f1;
    }

    public static bool SegmentIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector2 intersection)
    {
        float f1 = Mathf.Sign(Vector3.Cross(p2 - p1, p3 - p1).y);
        float f2 = Mathf.Sign(Vector3.Cross(p2 - p1, p4 - p1).y);
        float f3 = Mathf.Sign(Vector3.Cross(p4 - p3, p1 - p3).y);
        float f4 = Mathf.Sign(Vector3.Cross(p4 - p3, p2 - p3).y);
        intersection = Vector2.zero;
        if (f1 * f2 < 0 && f3 * f4 < 0)
        {
            Vector2 p21 = new Vector2(p1.x, p1.z);
            Vector2 p22 = new Vector2(p2.x, p2.z);
            Vector2 p23 = new Vector2(p3.x, p3.z);
            Vector2 p24 = new Vector2(p4.x, p4.z);
            MathHelpers.LineSegmentsIntersection(p21, p22, p23, p24, out intersection);
            return true;
        }
        return false;
    }


    public static bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }
}

