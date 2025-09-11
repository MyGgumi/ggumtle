package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.Exit;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.List;

public record ExitOpen(
        List<Exit> exits
) implements Body {
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(4 + (4 + (4 + 4 + 4) * 2) * exits.size());

        buffer.putInt(exits.size());
        for (Exit exit : exits) {
            buffer.putInt(exit.id);
            buffer.putInt(exit.leftTop.x).putInt(exit.leftTop.y).putInt(exit.leftTop.z);
            buffer.putInt(exit.rightBottom.x).putInt(exit.rightBottom.y).putInt(exit.rightBottom.z);
        }

        return buffer.array();
    }
}
