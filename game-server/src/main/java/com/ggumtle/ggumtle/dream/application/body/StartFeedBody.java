package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record StartFeedBody(
        Result result
) implements Body {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        FAIL((byte) 0), START_FEEDING((byte) 1),
        NOT_FOUND((byte) 2), YET_DIG_UP((byte) 3), ALREADY_DONE((byte) 4), LACK_OF_FEED_ITEM((byte) 5);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{this.result.value};
    }
}
