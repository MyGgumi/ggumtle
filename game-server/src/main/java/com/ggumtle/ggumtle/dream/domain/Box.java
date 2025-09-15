package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.session.Session;
import lombok.ToString;
import lombok.extern.slf4j.Slf4j;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@ToString
@Slf4j
public class Box {
    public static final int BOX_SIZE = 9;

    public final int id;

    public final Position position;

    private final Item[] items;
    private final Map<Item, Integer> itemCount;
    private int totalCount;
    private final Object itemLock;

    private final List<Session> viewer;

    public Box(int id, Position position) {
        this.id = id;
        this.position = position;

        this.items = new Item[BOX_SIZE];
        this.itemCount = new HashMap<>();
        this.totalCount = 0;
        this.itemLock = new Object();

        this.viewer = new ArrayList<>();
    }
    
    public boolean addItemBySystem(Item item, int count) {
        synchronized (itemLock) {
            int nextItemCount = this.itemCount.getOrDefault(item, 0) + count;

            if (this.totalCount + count > BOX_SIZE || item.maxCapacityForBox < nextItemCount) {
                return false;
            }

            this.totalCount += count;
            this.itemCount.put(item, nextItemCount);
            int leftCount = count;
            for (int i = 0; i < BOX_SIZE; i++) {
                if (this.items[i] == null) {
                    this.items[i] = item;
                    leftCount--;
                }

                if (leftCount <= 0) {
                    return true;
                }
            }

            log.warn("Item Total Count를 확인한 후 {}을 추가하였는데, 총 {}개 중 {}개의 아이템을 추가하는데 실패하였습니다", item, count, leftCount);
            return false;
        }
    }

    public boolean addItem(Item item) {
        synchronized (itemLock) {
            int nextItemCount = this.itemCount.getOrDefault(item, 0) + 1;

            if (this.totalCount + 1 > BOX_SIZE) {
                return false;
            }

            this.totalCount++;
            this.itemCount.put(item, nextItemCount);
            for (int i = 0; i < BOX_SIZE; i++) {
                if (this.items[i] == null) {
                    this.items[i] = item;
                    return true;
                }
            }
        }

        log.warn("Item Total Count를 확인한 후 {}을 추가하였는데 아이템을 추가하는데 실패하였습니다", item);
        return true;
    }

    public Item[] getItems() {
        synchronized (itemLock) {
            return Arrays.copyOf(this.items, this.items.length);
        }
    }

    public Item getItemAt(int index) {
        synchronized (itemLock) {
            return this.items[index];
        }
    }

    public boolean isFull() {
        synchronized (itemLock) {
            return this.totalCount >= BOX_SIZE;
        }
    }

    public Item[] popItem(int index) {
        synchronized (itemLock) {
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
    }

    public void addViewer(Session session) {
        synchronized (this.viewer) {
            this.viewer.add(session);
        }
    }

    public List<Session> getViewers() {
        synchronized (this.viewer) {
            return Collections.unmodifiableList(this.viewer);
        }
    }

    public void removeViewer(Session session) {
        synchronized (this.viewer) {
            this.viewer.remove(session);
        }
    }
}
