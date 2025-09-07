package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.Getter;
import lombok.ToString;

import java.util.Arrays;
import java.util.HashMap;
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

    public Box(int id, Position position) {
        this.id = id;
        this.position = position;

        this.items = new Item[BOX_SIZE];
        this.itemCount = new HashMap<>();
        this.totalCount = 0;
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

    public synchronized Item[] getItems() {
        return Arrays.copyOf(this.items, this.items.length);
    }
}
