package ggumtle.ggumtle.dreamtest.event;

import java.nio.ByteBuffer;

public record ShowBoxEvent(
        boolean success,
        int boxId,
        int[] items
) {
    private static final int BOX_SIZE = 9;

    public static ShowBoxEvent of(byte[] data) {
        if (data == null || data.length < 9) {
            return new ShowBoxEvent(false, -1, new int[BOX_SIZE]);
        }

        ByteBuffer buffer = ByteBuffer.wrap(data);

        boolean success = buffer.get() == 1;

        int boxId = buffer.getInt();
        int itemCount = buffer.getInt();
        int[] items = new int[BOX_SIZE];

        for (int i = 0; i < BOX_SIZE; i++) {
            items[i] = buffer.getInt();
        }

        return new ShowBoxEvent(success, boxId, items);
    }
}
