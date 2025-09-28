package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.ExitSpawn;
import com.ggumtle.ggumtle.dream.vo.FieldItemSpawn;
import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
import com.ggumtle.ggumtle.dream.vo.PlayerSpawn;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class SpawnCache {
    private final List<BoxSpawn> boxSpawns;
    private final List<GgumtleSpawn> ggumtleSpawns;
    private final List<PlayerSpawn> playerSpawns;
    private final List<ExitSpawn> exitSpawns;
    private final List<FieldItemSpawn> fieldItemSpawns;

    @Autowired
    public SpawnCache(
            BoxSpawnRepository boxSpawnRepository,
            GgumtleSpawnRepository ggumtleSpawnRepository,
            PlayerSpawnRepository playerSpawnRepository,
            ExitSpawnRepository exitSpawnRepository,
            FieldItemSpawnRepository fieldItemSpawnRepository) {
        this.boxSpawns = boxSpawnRepository.findAll();
        this.ggumtleSpawns = ggumtleSpawnRepository.findAll();
        this.playerSpawns = playerSpawnRepository.findAll();
        this.exitSpawns = exitSpawnRepository.findAll();
        this.fieldItemSpawns = fieldItemSpawnRepository.findAll();
    }

    public List<BoxSpawn> getRandomBoxSpawns(int size) {
        List<BoxSpawn> copy = new ArrayList<>(boxSpawns);

        Collections.shuffle(copy);

        return copy.subList(0, size);
    }

    public List<GgumtleSpawn> getRandomGgumtleSpawns(int size) {
        List<GgumtleSpawn> copy = new ArrayList<>(ggumtleSpawns);

        Collections.shuffle(copy);

        return copy.subList(0, size);
    }

    public List<PlayerSpawn> getRandomPlayerSpawns(int size) {
        List<PlayerSpawn> copy = new ArrayList<>(playerSpawns);

        Collections.shuffle(copy);

        return copy.subList(0, size);
    }

    public List<ExitSpawn> getExitSpawns() {
        return List.of(
                new ExitSpawn(1, 60 * 100,(int) (5.5 * 100),-10 * 100),
                new ExitSpawn(2, 42 * 100,5 * 100,20 * 100)
        );
    }

    public List<FieldItemSpawn> getFieldItemSpawns() {
        return List.copyOf(fieldItemSpawns);
    }
}
