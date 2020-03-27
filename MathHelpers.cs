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
}
