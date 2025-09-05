package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum CommonErrorCode implements ErrorCode {
    BAD_REQUEST("CM40000", "잘못된 요청입니다"),

    INTERNAL_SERVER_ERROR("CM50000", "알 수 없는 오류가 발생하였습니다"),
    ;

    private final String code;
    private final String message;

    public HttpStatus getHttpStatus() {
        return null;
    }
}
