package com.ggumtle.ggumtle.room.application.dto;

public record SceneChangeResult(
        Status status,
        Long roomId
) {
    public enum Status { FAIL, SUCCESS, DONE }
}
