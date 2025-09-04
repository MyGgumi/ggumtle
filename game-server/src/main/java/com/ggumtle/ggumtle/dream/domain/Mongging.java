package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;

import java.util.concurrent.ConcurrentHashMap;

public class Mongging extends Player {
    private static final int BASE_HP = 100;
    private static final int BASE_MOVE_SPEED = 100;
    private static final int BASE_HEAL_SPEED = 100;
    private static final int BASE_WORK_SPEED = 100;

    protected int hp;

    protected int moveSpeed;

    protected int healSpeed;

    protected int workSpeed;

    protected ConcurrentHashMap<Item, Integer> items;

    public Mongging(long id, Position position) {
        super(id, position);

        this.hp = BASE_HP;
        this.moveSpeed = BASE_MOVE_SPEED;
        this.healSpeed = BASE_HEAL_SPEED;
        this.workSpeed = BASE_WORK_SPEED;

        this.items = new ConcurrentHashMap<>();
    }
}
