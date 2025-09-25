package ggumtle.ggumtle.dreamtest.domain;

import lombok.Getter;

@Getter
public class Chest {
    private int id;
    private int x, y, z;
    private int[] items;

    public Chest(int id, int x, int y, int z) {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
        this.items = new int[9];
    }

}
