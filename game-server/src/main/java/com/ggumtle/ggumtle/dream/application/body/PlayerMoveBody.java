package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.vo.Position;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record PlayerMoveBody(
        long id,
        int x,
        int y,
        int z,
        int vx,
        int vy,
        int vz
) implements Body {
    public static PlayerMoveBody rollbackOf(long id, Position position) {
        return new PlayerMoveBody(id, position.x, position.y, position.z, -1, -1, -1);
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(8 + 4 * 3 * 2);

        buffer.putLong(id);
        buffer.putInt(x);
        buffer.putInt(y);
        buffer.putInt(z);
        buffer.putInt(vx);
        buffer.putInt(vy);
        buffer.putInt(vz);

        return buffer.array();
    }
}
