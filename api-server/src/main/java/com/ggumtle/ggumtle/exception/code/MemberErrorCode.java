package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum MemberErrorCode implements ErrorCode {
    NOT_FOUND("MB40401", "사용자를 찾을 수 없습니다"),
    ;

    private final String code;
    private final String message;

    public HttpStatus getHttpStatus() {
        return null;
    }
}
