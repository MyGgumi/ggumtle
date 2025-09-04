package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
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

    @Autowired
    public SpawnCache(BoxSpawnRepository boxSpawnRepository, GgumtleSpawnRepository ggumtleSpawnRepository) {
        this.boxSpawns = boxSpawnRepository.findAll();
        this.ggumtleSpawns = ggumtleSpawnRepository.findAll();
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
}
