package com.ggumtle.ggumtle.dream.util;

import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.dream.vo.Vector;

public class VectorCalculator {
    public static boolean calculateHit(Position source, Position target, Vector vector, int range, int targetSize) {
        double lx = source.x - target.x;
        double ly = source.y - target.y;
        double lz = source.z - target.z;

        double a = 1.0;
        double b = 2 * (vector.x * lx + vector.y * ly + vector.z * lz);
        double c = lx * lx + ly * ly + lz * lz - targetSize * targetSize;

        double discriminant = b * b - 4 * a * c;

        if (discriminant < 0) {
            return false;
        }

        double sqrtDisc = Math.sqrt(discriminant);
        double t1 = (-b - sqrtDisc) / 2.0;
        double t2 = (-b + sqrtDisc) / 2.0;

        return (t1 >= 0 && t1 <= range) || (t2 >= 0 && t2 <= range);
    }
}
