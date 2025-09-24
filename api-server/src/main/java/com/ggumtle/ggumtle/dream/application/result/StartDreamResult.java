package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.dream.domain.Dream;

public record StartDreamResult(
        START_DREAM_STATUS status,
        Long roomId,
        DreamServer dreamServer
) {
    private StartDreamResult(START_DREAM_STATUS status, Long roomId, String host, Integer port) {
        this(status, roomId, new DreamServer(host, port));
    }

    public enum START_DREAM_STATUS {
        RECEIVED, START_MATCH, WAITING, MATCHED, CREATE_ROOM, START_DREAM
    }

    public static StartDreamResult beforeRoomCreation(START_DREAM_STATUS status) {
        return new StartDreamResult(status, null, null);
    }

    public static StartDreamResult afterRoomCreation(START_DREAM_STATUS status, Dream dream) {
        return new StartDreamResult(status, dream.getRoomId(), new DreamServer(dream.getHost(), dream.getPort()));
    }

    public record DreamServer(
            String host,
            Integer port
    ) {
    }
}
