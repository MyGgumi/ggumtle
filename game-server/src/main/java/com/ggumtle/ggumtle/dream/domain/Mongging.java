package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public class Mongging extends Player {
    private static final int BASE_HP = 100;
    private static final int BASE_MOVE_SPEED = 100;
    private static final int BASE_HEAL_SPEED = 100;
    private static final int BASE_WORK_SPEED = 100;
    private static final int INVENTORY_SIZE = 3;
    private static final int ITEM_ID = 0;
    private static final int ITEM_COUNT = 1;
    private static final int MAX_KNOCKOUT_COUNT = 3;

    protected int hp;
    protected Status status;

    protected int moveSpeed;
    protected int healSpeed;
    protected int workSpeed;

    protected int knockOutCount = 0;

    private final ConcurrentHashMap <Item, Integer> inventory;
    private final ConcurrentHashMap<Item, Integer> droppedItems;

    public enum Status { ALIVE, KNOCKOUT, DEAD, ESCAPED }

    public Mongging(long id, Position position) {
        super(id, position);

        this.hp = BASE_HP;
        this.status = Status.ALIVE;

        this.moveSpeed = BASE_MOVE_SPEED;
        this.healSpeed = BASE_HEAL_SPEED;
        this.workSpeed = BASE_WORK_SPEED;

        this.inventory = new ConcurrentHashMap<>();
        this.droppedItems = new ConcurrentHashMap<>();
    }

    public synchronized int getHit(int damage) {
        if (this.hp > damage) {
            this.hp -= damage;
            return this.hp;
        }

        this.hp = 0;
        this.droppedItems.clear();
        this.droppedItems.putAll(this.inventory);
        this.inventory.clear();
        this.status = Status.KNOCKOUT;
        this.knockOutCount++;

        if (this.knockOutCount > MAX_KNOCKOUT_COUNT) {
            this.status = Status.DEAD;
        }

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

    public synchronized Map<Item, Integer> getDroppedItems() {
        return new HashMap<>(this.droppedItems);
    }

    public boolean isNotDead() {
        return this.status == Status.ALIVE && this.hp > 0;
    }

    public boolean isDead() {
        return this.status == Status.DEAD;
    }

    public synchronized void escape() {
        this.status = Status.ESCAPED;
    }
}
