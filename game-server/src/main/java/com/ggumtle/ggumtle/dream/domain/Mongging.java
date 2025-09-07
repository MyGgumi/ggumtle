package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;

public class Mongging extends Player {
    private static final int BASE_HP = 100;
    private static final int BASE_MOVE_SPEED = 100;
    private static final int BASE_HEAL_SPEED = 100;
    private static final int BASE_WORK_SPEED = 100;
    public static final int INVENTORY_SIZE = 3;
    private static final int ITEM_ID = 0;
    private static final int ITEM_COUNT = 1;

    protected int hp;

    protected int moveSpeed;
    protected int healSpeed;
    protected int workSpeed;

    private int[][] inventory;

    public Mongging(long id, Position position) {
        super(id, position);

        this.hp = BASE_HP;
        this.moveSpeed = BASE_MOVE_SPEED;
        this.healSpeed = BASE_HEAL_SPEED;
        this.workSpeed = BASE_WORK_SPEED;

        this.inventory = new int[INVENTORY_SIZE][2];
    }

    public synchronized int getHit(int damage) {
        if (this.hp > damage) {
            this.hp -= damage;
            return this.hp;
        }

        this.hp = 0;
        this.inventory = new int[INVENTORY_SIZE][2];
        return this.hp;
    }

    public synchronized boolean canAddItem(Item item) {
        boolean hasEmptyCell = false;

        for (int i = 0; i < INVENTORY_SIZE; i++) {
            if (inventory[i][ITEM_ID] == item.getId()) {
                return inventory[i][ITEM_COUNT] + 1 >= item.getMaxCapacityForMongging();
            }

            if (inventory[i][ITEM_COUNT] == 0) {
                hasEmptyCell = true;
            }
        }

        return hasEmptyCell;
    }

    public synchronized boolean addItem(Item item) {
        int emptyCellIndex = -1;

        for (int i = 0; i < INVENTORY_SIZE; i++) {
            if (inventory[i][ITEM_ID] == item.getId()) {
                if (inventory[i][ITEM_COUNT] + 1 >= item.getMaxCapacityForMongging()) {
                    return false;
                }
                inventory[i][ITEM_COUNT]++;
                return false;
            }

            if (inventory[i][ITEM_COUNT] == 0) {
                emptyCellIndex = i;
            }
        }

        if (emptyCellIndex == -1) {
            return false;
        }

        inventory[emptyCellIndex][ITEM_ID] = item.getId();
        inventory[emptyCellIndex][ITEM_COUNT] = 1;
        return true;
    }

    public synchronized int[][] getItems() {
        int[][] copy = new int[INVENTORY_SIZE][2];
        for (int i = 0; i < INVENTORY_SIZE; i++) {
            copy[i][0] = this.inventory[i][0];
            copy[i][1] = this.inventory[i][1];
        }
        return copy;
    }

    public synchronized Item getItemAt(int index) {
        if (inventory[index][ITEM_COUNT] == 0) {
            return null;
        }

        return Item.valueOf(inventory[index][ITEM_ID]);
    }

    public synchronized Item popItem(int index) {
        if (inventory[index][ITEM_COUNT] == 0) {
            return null;
        }

        inventory[index][ITEM_COUNT]--;
        return Item.valueOf(inventory[index][ITEM_ID]);
    }
}
