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
        NOT_FOUND_GGUMTLE((byte) 2), NOT_FOUND_PLAYER((byte) 3),
        YET_DIG_UP((byte) 10), ALREADY_DONE((byte) 11), LACK_OF_FEED_ITEM((byte) 12), NOT_AROUND((byte) 13);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{this.result.value};
    }
}
