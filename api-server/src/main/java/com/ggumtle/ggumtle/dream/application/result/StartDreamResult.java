package com.ggumtle.ggumtle.dream.application.result;

public record StartDreamResult(
        START_DREAM_STATUS status,
        Dream dream
) {
    public StartDreamResult(START_DREAM_STATUS status, String roomId, String dreamServerId) {
        this(status, new Dream(roomId, dreamServerId));
    }

    public enum START_DREAM_STATUS {
        RECEIVED, WAITING, MATCHED, CREATE_ROOM, START
    }

    public record Dream(
            String roomId,
            String dreamServerId
    ) {
    }
}
