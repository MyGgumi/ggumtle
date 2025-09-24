package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum MissionErrorCode implements ErrorCode {
    NOT_MISSION_OWNER(HttpStatus.FORBIDDEN, "MM40301", "미션의 주인이 아닙니다"),
    WITHDRAW_MISSION(HttpStatus.FORBIDDEN,"MM40302", "탈퇴한 회원의 미션입니다"),

    NOT_FOUND_MEMBER_MISSION(HttpStatus.NOT_FOUND, "MM40401", "사용자의 미션을 찾을 수 없습니다"),

    CONFLICT_MISSION_STATE_FOR_REWARD(HttpStatus.CONFLICT, "MM40901", "해당 미션의 상태는 보상을 수령할 수 없습니다")
    ;

    private final HttpStatus httpStatus;
    private final String code;
    private final String message;
}
