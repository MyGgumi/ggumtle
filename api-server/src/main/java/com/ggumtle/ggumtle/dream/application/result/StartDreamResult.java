package com.ggumtle.ggumtle.dream.application.result;

public record StartDreamResult(
        START_DREAM_STATUS status,
        Dream dream
) {
    public StartDreamResult(START_DREAM_STATUS status, Long roomId, String dreamServerId) {
        this(status, new Dream(roomId, dreamServerId));
    }

    public enum START_DREAM_STATUS {
        RECEIVED, START_MATCH, WAITING, MATCHED, CREATE_ROOM, START_DREAM
    }

    public record Dream(
            Long roomId,
            String dreamServerId
    ) {
    }
}
