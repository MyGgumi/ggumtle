package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record StartFeedResult(
        Status result
) implements Result {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        FAIL((byte) 0), START_FEEDING((byte) 1),
        NOT_FOUND((byte) 2), YET_DIG_UP((byte) 3), ALREADY_DONE((byte) 4), LACK_OF_FEED_ITEM((byte) 5);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{this.result.value};
    }
}
