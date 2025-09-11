package com.ggumtle.ggumtle.dream.vo;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import lombok.Getter;

@Entity
@Getter
public class FieldItemSpawn implements Spawn {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int id;

    private int x;

    private int y;

    private int z;

    private int typeId;
}
