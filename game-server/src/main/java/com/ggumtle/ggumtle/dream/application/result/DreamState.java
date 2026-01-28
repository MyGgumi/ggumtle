package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.dream.domain.ggumtle.Ggumtle;
import com.ggumtle.ggumtle.dream.domain.item.Box;
import com.ggumtle.ggumtle.dream.domain.item.FieldItem;
import com.ggumtle.ggumtle.dream.domain.player.Player;

import java.util.List;

public record DreamState(
        List<Box> boxes,
        List<Ggumtle> ggumtles,
        List<FieldItem> healPacks,
        List<FieldItem> speedPacks,
        List<Player> players
) {
}
