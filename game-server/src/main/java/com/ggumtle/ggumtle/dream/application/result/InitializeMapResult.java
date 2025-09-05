package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Ggumtle;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.List;

public record InitializeMapResult(
        List<Box> boxes,
        List<Ggumtle> ggumtles,
        List<Object> healPacks,
        List<Object> speedPacks
) implements Result {
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
                        + countInfoSize + healPackInfoSize * 3
                        + countInfoSize + speedPackInfoSize * 5
        );

        buffer.putInt(boxes.size());
        for (Box box : boxes) {
            buffer.putInt(box.getId());
            buffer.putInt(box.getPosition().x);
            buffer.putInt(box.getPosition().y);
            buffer.putInt(box.getPosition().z);
        }

        buffer.putInt(ggumtles.size());
        for (Ggumtle ggumtle : ggumtles) {
            buffer.putInt(ggumtle.getId());
            buffer.putInt(ggumtle.getPosition().x);
            buffer.putInt(ggumtle.getPosition().y);
            buffer.putInt(ggumtle.getPosition().z);
        }

        // TODO: 힐팩과 스피드팩 초기화 구현 후 실제 데이터로 수정
        buffer.putInt(3);
        for (int i = 0; i < 3; i++) {
            buffer.putInt(0);
            buffer.putInt(0);
            buffer.putInt(0);
            buffer.putInt(0);
        }

        buffer.putInt(5);
        for (int i = 0; i < 5; i++) {
            buffer.putInt(0);
            buffer.putInt(0);
            buffer.putInt(0);
            buffer.putInt(0);
        }

        return buffer.array();
    }
}
