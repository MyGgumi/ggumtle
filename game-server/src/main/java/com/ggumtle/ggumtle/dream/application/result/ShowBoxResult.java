package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.vo.Item;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record ShowBoxResult(
        boolean success,
        int boxId,
        Item[] items
) implements Result {
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer byteBuffer = ByteBuffer.allocate(1 + 4 + 4 + 4 * Box.BOX_SIZE);

        byteBuffer.put(success ? (byte) 1 : (byte) 0);

        if (!success) {
            byteBuffer.putInt(-1);
            byteBuffer.putInt(-1);
            for (int i = 0; i < Box.BOX_SIZE; i++) {
                byteBuffer.putInt(-1);
            }

            return byteBuffer.array();
        }

        byteBuffer.putInt(boxId);
        byteBuffer.putInt(items.length);
        for (Item item : items) {
            byteBuffer.putInt(item == null ? -1 : item.getId());
        }

        return byteBuffer.array();
    }
}
