package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.session.Session;
import lombok.Getter;
import lombok.ToString;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@ToString
public class Box {
    public static final int BOX_SIZE = 9;

    @Getter
    private final int id;

    @Getter
    private final Position position;

    private final Item[] items;
    private final Map<Item, Integer> itemCount;
    private int totalCount;

    private final List<Session> viewer;

    public Box(int id, Position position) {
        this.id = id;
        this.position = position;

        this.items = new Item[BOX_SIZE];
        this.itemCount = new HashMap<>();
        this.totalCount = 0;

        this.viewer = new ArrayList<>();
    }
    
    public synchronized boolean addItemBySystem(Item item, int count) {
        int nextItemCount = this.itemCount.getOrDefault(item, 0) + count;

        if (this.totalCount + count > BOX_SIZE || item.getMaxCapacityForBox() < nextItemCount) {
            return false;
        }

        this.totalCount += count;
        this.itemCount.put(item, nextItemCount);
        for (int i = 0; i < BOX_SIZE; i++) {
            if (this.items[i] == null) {
                this.items[i] = item;
                count--;
            }

            if (count <= 0) {
                break;
            }
        }
        return true;
    }

    public synchronized boolean addItem(Item item) {
        int nextItemCount = this.itemCount.getOrDefault(item, 0) + 1;

        if (this.totalCount + 1 > BOX_SIZE || item.getMaxCapacityForBox() < nextItemCount) {
            return false;
        }

        this.totalCount++;
        this.itemCount.put(item, nextItemCount);
        for (int i = 0; i < BOX_SIZE; i++) {
            if (this.items[i] == null) {
                this.items[i] = item;
                break;
            }
        }

        return true;
    }

    public synchronized Item[] getItems() {
        return Arrays.copyOf(this.items, this.items.length);
    }

    public synchronized Item getItemAt(int index) {
        return this.items[index];
    }

    public synchronized boolean canAddItem(Item item) {
        if (totalCount == BOX_SIZE) {
            return false;
        }

        return itemCount.getOrDefault(item, 0) + 1 > item.getMaxCapacityForBox();
    }

    public synchronized Item[] popItem(int index) {
        Item item = this.items[index];
        if (item == null) {
            return null;
        }

        this.items[index] = null;
        this.itemCount.put(item, itemCount.get(item) - 1);
        this.totalCount--;

        Item[] result = new Item[BOX_SIZE + 1];
        for (int i = 0; i < BOX_SIZE; i++) {
            result[i] = this.items[i];
        }
        result[BOX_SIZE] = item;

        return result;
    }

    public synchronized void addViewer(Session session) {
        this.viewer.add(session);
    }

    public synchronized List<Session> getViewers() {
        return Collections.unmodifiableList(this.viewer);
    }

    public synchronized void removeViewer(Session session) {
        this.viewer.remove(session);
    }
}
