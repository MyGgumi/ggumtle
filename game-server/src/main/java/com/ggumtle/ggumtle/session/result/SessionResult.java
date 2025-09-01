package com.ggumtle.ggumtle.session.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.RequiredArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

@RequiredArgsConstructor
public class SessionResult implements Result {
    private final boolean success;
    private final long sessionId;

    @Override
    public byte[] toBytes(Charset charset) {
        ByteBuffer byteBuffer = ByteBuffer.allocate(1 + 8);

        byteBuffer.put(success ? (byte) 1 : (byte) 0);
        byteBuffer.putLong(sessionId);

        return byteBuffer.array();
    }
}
