package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.Getter;
import lombok.ToString;

import java.util.concurrent.ConcurrentHashMap;

@Getter
@ToString
public class Box {
    private static final int MAX_ITEM_SIZE = 4;

    private final int id;

    private final Position position;

    private final ConcurrentHashMap<Item, Integer> items;
    private int totalCount;

    public Box(int id, Position position) {
        this.id = id;
        this.position = position;

        this.items = new ConcurrentHashMap<>();
        this.totalCount = 0;
    }
    
    public synchronized boolean addItemBySystem(Item item, int count) {
        int nextCount = items.getOrDefault(item, 0) + count;

        if (this.totalCount + count > MAX_ITEM_SIZE || item.getMaxCapacityForBox() < nextCount) {
            return false;
        }

        this.items.put(item, nextCount);
        this.totalCount += count;
        return true;
    }
}
