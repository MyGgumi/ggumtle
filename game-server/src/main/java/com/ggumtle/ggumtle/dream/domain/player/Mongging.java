package com.ggumtle.ggumtle.dream.domain.player;

import com.ggumtle.ggumtle.dream.application.result.GetHitResult;
import com.ggumtle.ggumtle.dream.domain.item.Boxable;
import com.ggumtle.ggumtle.dream.domain.item.ItemDictionary;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.room.domain.PlayerInfo;

import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public class Mongging extends Player {
    private static final int BASE_HP = 100;
    private static final int REVIVE_HP = 50;
    private static final int BASE_MOVE_SPEED = 1_000_000_000;
    private static final int BASE_HEAL_SPEED = 100;
    private static final int BASE_WORK_SPEED = 100;
    private static final int INVENTORY_SIZE = 3;
    private static final int ITEM_ID = 0;
    private static final int ITEM_COUNT = 1;
    private static final int MAX_KNOCKOUT_COUNT = 3;
    public static final long REVIVE_DISTANCE_SQUARE = 1_000_000_000L * 1_000_000_000L;

    public final long classId;
    public final int maxHp;
    public final int healSpeed;
    public final int workSpeed;

    private int hp;
    private Status status;
    private int knockOutCount;
    private final Object statusLock;

    private final ConcurrentHashMap<Boxable, Integer> inventory;
    private final ConcurrentHashMap<Boxable, Integer> droppedItems;
    private final Object inventoryLock;

    public enum Status { ALIVE, KNOCKOUT, DEAD, ESCAPED }

    public Mongging(long id, Position position, PlayerInfo playerInfo) {
        super(id, position, BASE_MOVE_SPEED);

        this.classId = playerInfo.monggingClassId;
        this.maxHp = BASE_HP + playerInfo.additionalHp;
        this.healSpeed = BASE_HEAL_SPEED + playerInfo.additionalHealSpeed;
        this.workSpeed = BASE_WORK_SPEED + playerInfo.additionalTaskSpeed;

        this.hp = maxHp;
        this.knockOutCount = 0;
        this.status = Status.ALIVE;
        this.statusLock = new Object();

        this.inventory = new ConcurrentHashMap<>();
        this.droppedItems = new ConcurrentHashMap<>();
        this.inventoryLock = new Object();
    }

    public GetHitResult getHit(int damage) {
        synchronized (statusLock) {
            synchronized (inventoryLock) {
                if (this.status != Status.ALIVE) {
                    return new GetHitResult(GetHitResult.Result.NOT_ALIVE, -1);
                }

                if (this.hp > damage) {
                    this.hp -= damage;
                    return new GetHitResult(GetHitResult.Result.ALIVE, this.hp);
                }

                this.hp = 0;
                this.knockOutCount++;

                this.droppedItems.clear();
                Iterator<Boxable> itemIterator = this.inventory.keySet().iterator();
                while (itemIterator.hasNext()) {
                    Boxable item = itemIterator.next();

                    if (item.id != ItemDictionary.DEFIBRILLATOR.boxableItem.id) {
                        this.droppedItems.put(item, this.inventory.get(item));
                        itemIterator.remove();
                    }
                }

                if (this.knockOutCount > MAX_KNOCKOUT_COUNT) {
                    this.status = Status.DEAD;
                    return new GetHitResult(GetHitResult.Result.DEAD, this.hp);
                } else {
                    this.status = Status.KNOCKOUT;
                    return new GetHitResult(GetHitResult.Result.KNOCK_OUT, this.hp);
                }
            }
        }
    }

    public boolean canAddItem(Boxable item) {
        synchronized (inventoryLock) {
            if (this.inventory.containsKey(item)) {
                return this.inventory.get(item) + 1 <= item.maxCapacityForMongging;
            }

            return this.inventory.size() < INVENTORY_SIZE;
        }
    }

    public boolean addItem(Boxable item) {
        synchronized (inventoryLock) {
            if (this.inventory.containsKey(item)) {
                int nextCount = this.inventory.get(item) + 1;
                if (nextCount > item.maxCapacityForMongging) {
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
    }

    public int[][] getItems() {
        synchronized (inventoryLock) {
            int[][] items = new int[this.inventory.size()][2];
            int top = 0;

            for (Map.Entry<Boxable, Integer> entry : this.inventory.entrySet()) {
                items[top][ITEM_ID] = entry.getKey().id;
                items[top][ITEM_COUNT] = entry.getValue();
                top++;
            }

            return items;
        }
    }

    public Boxable popItem(Boxable item) {
        synchronized (inventoryLock) {
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
    }

    public int countItem(Boxable item) {
        return this.inventory.getOrDefault(item, 0);
    }

    public Map<Boxable, Integer> getDroppedItems() {
        synchronized (inventoryLock) {
            return new HashMap<>(this.droppedItems);
        }
    }

    public boolean isNotDead() {
        synchronized (statusLock) {
            return this.status == Status.ALIVE && this.hp > 0;
        }
    }

    public boolean isDead() {
        synchronized (statusLock) {
            return this.status == Status.DEAD;
        }
    }

    public boolean isKnockout() {
        synchronized (statusLock) {
            return this.status == Status.KNOCKOUT;
        }
    }

    public boolean isEscaped() {
        synchronized (statusLock) {
            return this.status == Status.ESCAPED;
        }
    }

    public int useDefibrillator() {
        synchronized (statusLock) {
            synchronized (inventoryLock) {
                if (this.status != Status.KNOCKOUT) {
                    return -1;
                }

                Boxable defibrillator = ItemDictionary.DEFIBRILLATOR.boxableItem;
                int count = this.inventory.getOrDefault(defibrillator, 0);
                if (count == 0) {
                    return -2;
                }

                if (count == 1) {
                    this.inventory.remove(defibrillator);
                } else {
                    this.inventory.put(defibrillator, count - 1);
                }
                this.hp = REVIVE_HP;
                this.status = Status.ALIVE;
                return this.hp;
            }
        }
    }

    public void revive() {
        synchronized (statusLock) {
            this.hp = REVIVE_HP;
            this.status = Status.ALIVE;
        }
    }

    public void escape() {
        synchronized (statusLock) {
            this.status = Status.ESCAPED;
        }
    }
}
