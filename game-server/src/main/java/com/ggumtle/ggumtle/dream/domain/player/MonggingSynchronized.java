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

public class MonggingSynchronized extends Player {
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

    private final ConcurrentHashMap<Boxable, Integer> inventory;
    private final ConcurrentHashMap<Boxable, Integer> droppedItems;

    public enum Status { ALIVE, KNOCKOUT, DEAD, ESCAPED }

    public MonggingSynchronized(long id, Position position, PlayerInfo playerInfo) {
        super(id, position, BASE_MOVE_SPEED);

        this.classId = playerInfo.monggingClassId;
        this.maxHp = BASE_HP + playerInfo.additionalHp;
        this.healSpeed = BASE_HEAL_SPEED + playerInfo.additionalHealSpeed;
        this.workSpeed = BASE_WORK_SPEED + playerInfo.additionalTaskSpeed;

        this.hp = maxHp;
        this.knockOutCount = 0;
        this.status = Status.ALIVE;

        this.inventory = new ConcurrentHashMap<>();
        this.droppedItems = new ConcurrentHashMap<>();
    }

    public synchronized GetHitResult getHit(int damage) {
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

    public boolean canAddItem(Boxable item) {
        if (this.inventory.containsKey(item)) {
            return this.inventory.get(item) + 1 <= item.maxCapacityForMongging;
        }

        return this.inventory.size() < INVENTORY_SIZE;
    }

    public boolean addItem(Boxable item) {
        // ConcurrentHashMap의 atomic 연산 활용
        return this.inventory.compute(item, (key, currentCount) -> {
            if (currentCount == null) {
                // 새 아이템 추가
                if (this.inventory.size() < INVENTORY_SIZE) {
                    return 1;
                }
                return null; // 추가 실패
            } else {
                // 기존 아이템 증가
                int nextCount = currentCount + 1;
                if (nextCount <= item.maxCapacityForMongging) {
                    return nextCount;
                }
                return currentCount; // 변경 없음
            }
        }) != null;
    }

    public synchronized int[][] getItems() {
        int[][] items = new int[this.inventory.size()][2];
        int top = 0;

        for (Map.Entry<Boxable, Integer> entry : this.inventory.entrySet()) {
            items[top][ITEM_ID] = entry.getKey().id;
            items[top][ITEM_COUNT] = entry.getValue();
            top++;
        }

        return items;
    }

    public Boxable popItem(Boxable item) {
        // ConcurrentHashMap의 atomic 연산 활용
        Integer result = this.inventory.compute(item, (key, currentCount) -> {
            if (currentCount == null || currentCount <= 0) {
                return null; // 제거 실패
            }

            int nextCount = currentCount - 1;
            return nextCount <= 0 ? null : nextCount; // 0이면 제거, 아니면 감소
        });

        return result != null || this.inventory.containsKey(item) ? item : null;
    }

    public int countItem(Boxable item) {
        return this.inventory.getOrDefault(item, 0);
    }

    public Map<Boxable, Integer> getDroppedItems() {
        return new HashMap<>(this.droppedItems); // ConcurrentHashMap 복사는 thread-safe
    }

    public synchronized boolean isNotDead() {
        return this.status == Status.ALIVE && this.hp > 0;
    }

    public synchronized boolean isDead() {
        return this.status == Status.DEAD;
    }

    public synchronized boolean isKnockout() {
        return this.status == Status.KNOCKOUT;
    }

    public synchronized boolean isEscaped() {
        return this.status == Status.ESCAPED;
    }

    public synchronized int useDefibrillator() {
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

    public synchronized void revive() {
        this.hp = REVIVE_HP;
        this.status = Status.ALIVE;
    }

    public synchronized void escape() {
        this.status = Status.ESCAPED;
    }
}