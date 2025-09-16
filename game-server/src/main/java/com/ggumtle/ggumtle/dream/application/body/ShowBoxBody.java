package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.item.Box;
import com.ggumtle.ggumtle.dream.domain.item.Boxable;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record ShowBoxBody(
        boolean success,
        int boxId,
        Boxable[] items
) implements Body {
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
        for (Boxable item : items) {
            byteBuffer.putInt(item == null ? -1 : item.id);
        }

        return byteBuffer.array();
    }
}
