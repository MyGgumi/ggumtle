package com.ggumtle.ggumtle.dream.util;

import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Item;

import java.util.List;
import java.util.Random;

public class ItemDistributor {
    public static void distribute(Item item, List<Box> boxes, int totalCount) {
        Random random = new Random();

        int remaining = totalCount;
        while (remaining > 0) {
            int i = random.nextInt(boxes.size());

            boolean success = boxes.get(i).addItemBySystem(item, 1);
            if (success) {
                remaining--;
            }
        }
    }
}
