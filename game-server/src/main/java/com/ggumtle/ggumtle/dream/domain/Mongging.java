package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

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

    private final ConcurrentHashMap <Item, Integer> inventory;

    public Mongging(long id, Position position) {
        super(id, position);

        this.hp = BASE_HP;
        this.moveSpeed = BASE_MOVE_SPEED;
        this.healSpeed = BASE_HEAL_SPEED;
        this.workSpeed = BASE_WORK_SPEED;

        this.inventory = new ConcurrentHashMap<>();
    }

    public synchronized int getHit(int damage) {
        if (this.hp > damage) {
            this.hp -= damage;
            return this.hp;
        }

        this.hp = 0;
        this.inventory.clear();
        return this.hp;
    }

    public synchronized boolean canAddItem(Item item) {
        if (this.inventory.containsKey(item)) {
            return this.inventory.get(item) + 1 <= item.getMaxCapacityForMongging();
        }

        return this.inventory.size() < INVENTORY_SIZE;
    }

    public synchronized boolean addItem(Item item) {
        if (this.inventory.containsKey(item)) {
            int nextCount = this.inventory.get(item) + 1;
            if (nextCount > item.getMaxCapacityForMongging()) {
                return false;
            }

            this.inventory.put(item, nextCount);
            return true;
        }

        if (this.inventory.size() < INVENTORY_SIZE) {
            this.inventory.put(item, 1);
            return true;
        }

        return false;
    }

    public synchronized int[][] getItems() {
        int[][] items = new int[this.inventory.size()][2];
        int top = 0;

        for (Map.Entry<Item, Integer> entry : this.inventory.entrySet()) {
            items[top][ITEM_ID] = entry.getKey().getId();
            items[top][ITEM_COUNT] = entry.getValue();
            top++;
        }

        return items;
    }

    public synchronized Item popItem(Item item) {
        if (!this.inventory.containsKey(item)) {
            return null;
        }

        int nextCount = this.inventory.get(item) - 1;

        if (nextCount <= 0) {
            this.inventory.remove(item);
        } else {
            this.inventory.put(item, nextCount);
        }

        return item;
    }

    public synchronized int countItem(Item item) {
        return this.inventory.getOrDefault(item, 0);
    }
}
