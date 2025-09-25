package ggumtle.ggumtle.dreamtest.event;

import java.nio.ByteBuffer;

public record TakeItemEvent(
        boolean success,
        long playerId,
        int boxId,
        int[] boxItems,
        int takenItem
) {
    private static final int BOX_SIZE = 9;
    
    public static TakeItemEvent of(byte[] data) {
        ByteBuffer buffer = ByteBuffer.wrap(data);
        
        int resultCode = buffer.getInt();
        boolean success = resultCode == 1;
        
        long playerId = buffer.getLong();
        
        int boxId = buffer.getInt();
        
        int boxSize = buffer.getInt();

        int[] boxItems = new int[boxSize];
        for (int i = 0; i < boxSize; i++) {
            boxItems[i] = buffer.getInt();
        }

        int takenItem = buffer.getInt();

        return new TakeItemEvent(success, playerId, boxId, boxItems, takenItem);
    }
    
    private static String getResultString(int resultCode) {
        return switch (resultCode) {
            case 1 -> "SUCCESS";
            case 0 -> "FAIL";
            case 2 -> "INDEX_OUT_OF_RANGE";
            case 3 -> "NOT_FOUND_BOX";
            case 4 -> "NOT_FOUND_PLAYER";
            case 5 -> "NOT_MONGGING";
            case 10 -> "NOT_FOUND_ITEM";
            case 11 -> "FULL_ABOUT_ITEM";
            case 12 -> "NOT_NEAR";
            default -> "UNKNOWN_" + resultCode;
        };
    }
}
