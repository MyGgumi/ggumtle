package ggumtle.ggumtle.dreamtest.event;

import java.nio.ByteBuffer;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;

public record PutItemEvent(
        Result result,
        int[] items
) {
    public static final int BOX_SIZE = 9;

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS(1), FAIL(0),
        ILLEGAL_ITEM_ID(2), NOT_FOUND_BOX(3), NOT_FOUND_PLAYER(4), NOT_MONGGING(5),
        NOT_FOUND_ITEM(10), FULL_ABOUT_ITEM(11), NOT_NEAR(12);

        public final int value;

        public static Result fromValue(int value) {
            for (Result r : values()) {
                if (r.value == value) return r;
            }
            return null;
        }
    }

    public static PutItemEvent of(byte[] data) {
        ByteBuffer buffer = ByteBuffer.wrap(data);

        int resultCode = buffer.getInt();
        Result result = Result.fromValue(resultCode);
        if (result == null) result = Result.FAIL;

        int[] items = new int[BOX_SIZE];

        if (result == Result.SUCCESS) {
            int boxSize = buffer.getInt();
            for (int i = 0; i < boxSize; i++) {
                items[i] = buffer.getInt();
            }
        } else {
            for (int i = 0; i < BOX_SIZE; i++) {
                items[i] = buffer.getInt();
            }
        }

        return new PutItemEvent(result, items);
    }
}
