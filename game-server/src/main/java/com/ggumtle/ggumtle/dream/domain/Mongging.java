package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Item;

import java.util.Map;

public class Mongging extends Player {
    protected int hp;

    protected Map<Item, Integer> items;

    public Mongging(long id, int hp, Map<Item, Integer> items) {
        super(id);
        this.hp = hp;
        this.items = items;
    }
}
