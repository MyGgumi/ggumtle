package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.domain.Mongdung;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record MongdungSkillResult(
        int skillTypeId,
        Status result
) implements Result {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        SUCCESS((byte) 1),
        NOT_FOUND_MONGDUNG((byte) 2), NOT_FOUND_SKILL((byte) 3),
        YET_COOL_TIME((byte) 10), LACK_USE_COUNT((byte) 11),
        ;

        private final byte value;
    }

    public MongdungSkillResult(Mongdung.SkillType skillType, Status result) {
        this(skillType.getId(), result);
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(4 + 1).putInt(skillTypeId).put(result.value).array();
    }
}
