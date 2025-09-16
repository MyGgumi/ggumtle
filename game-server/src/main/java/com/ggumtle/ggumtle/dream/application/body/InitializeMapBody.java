package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.item.Box;
import com.ggumtle.ggumtle.dream.domain.item.FieldItem;
import com.ggumtle.ggumtle.dream.domain.ggumtle.Ggumtle;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.List;

public record InitializeMapBody(
        List<Box> boxes,
        List<Ggumtle> ggumtles,
        List<FieldItem> healPacks,
        List<FieldItem> speedPacks
) implements Body {
    private static final int countInfoSize = 4;
    private static final int boxInfoSize = 4 + 4 + 4 + 4;
    private static final int ggumtleInfoSize = 4 + 4 + 4 + 4;
    private static final int healPackInfoSize = 4 + 4 + 4 + 4;
    private static final int speedPackInfoSize = 4 + 4 + 4 + 4;

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(
                countInfoSize + boxInfoSize * boxes.size()
                        + countInfoSize + ggumtleInfoSize * ggumtles.size()
                        + countInfoSize + healPackInfoSize * healPacks.size()
                        + countInfoSize + speedPackInfoSize * speedPacks.size()
        );

        buffer.putInt(boxes.size());
        for (Box box : boxes) {
            buffer.putInt(box.id);
            buffer.putInt(box.position.x);
            buffer.putInt(box.position.y);
            buffer.putInt(box.position.z);
        }

        buffer.putInt(ggumtles.size());
        for (Ggumtle ggumtle : ggumtles) {
            buffer.putInt(ggumtle.id);
            buffer.putInt(ggumtle.position.x);
            buffer.putInt(ggumtle.position.y);
            buffer.putInt(ggumtle.position.z);
        }

        buffer.putInt(healPacks.size());
        for (FieldItem healPack : healPacks) {
            buffer.putInt(healPack.id);
            buffer.putInt(healPack.position.x);
            buffer.putInt(healPack.position.y);
            buffer.putInt(healPack.position.z);
        }

        buffer.putInt(speedPacks.size());
        for (FieldItem speedPack : speedPacks) {
            buffer.putInt(speedPack.id);
            buffer.putInt(speedPack.position.x);
            buffer.putInt(speedPack.position.y);
            buffer.putInt(speedPack.position.z);
        }

        return buffer.array();
    }
}
