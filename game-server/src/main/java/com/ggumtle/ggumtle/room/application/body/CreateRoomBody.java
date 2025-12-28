package com.ggumtle.ggumtle.room.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record CreateRoomBody(
        Result result,
        long roomId
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS(1), FAIL(0);

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        // 4 bytes for result + 8 bytes for roomId = 12 bytes
        return ByteBuffer.allocate(12)
                .putInt(result.value)
                .putLong(roomId)
                .array();
    }
}
