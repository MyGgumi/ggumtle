package com.ggumtle.ggumtle.dream.vo;

public class Vector {
    public final double x;
    public final double y;
    public final double z;

    public Vector(int x, int y, int z) {
        double length = Math.sqrt(x * x + y * y + z * z);

        this.x = x / length;
        this.y = y / length;
        this.z = z / length;
    }
}
