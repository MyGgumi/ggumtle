package com.ggumtle.ggumtle.auth.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.RequiredArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

@RequiredArgsConstructor
public class AuthBody implements Body {
    private final boolean success;

    @Override
    public byte[] toBytes(Charset charset) {
        ByteBuffer byteBuffer = ByteBuffer.allocate(1);

        byteBuffer.put(success ? (byte) 1 : (byte) 0);

        return byteBuffer.array();
    }
}
