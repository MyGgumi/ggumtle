package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum DreamErrorCode implements ErrorCode {
    NOT_MY_INVITATION("DR40302", "자신이 받은 초대가 아닙니다"),
    NOT_LEADER("DR40303", "파티의 리더가 아닙니다"),

    NOT_FOUND_PARTY("DR40401", "파티를 찾을 수 없습니다"),
    NOT_FOUND_INVITATION("DR40402", "파티 초대를 찾을 수 없습니다"),

    ALREADY_IN_PARTY("DR40902", "이미 파티에 속해 있습니다"),
    ;

    private final String code;
    private final String message;

    public HttpStatus getHttpStatus() {
        return null;
    }
}
