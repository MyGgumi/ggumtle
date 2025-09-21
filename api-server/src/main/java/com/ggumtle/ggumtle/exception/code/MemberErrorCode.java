package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum MemberErrorCode implements ErrorCode {
    NOT_ENOUGH_COIN(HttpStatus.BAD_REQUEST, "MB40001", "코인이 부족합니다"),

    NOT_FOUND(HttpStatus.NOT_FOUND, "MB40401", "사용자를 찾을 수 없습니다"),

    DUPLICATE_NICKNAME(HttpStatus.CONFLICT, "MB40901", "중복 된 닉네임입니다")
    ;

    private final HttpStatus httpStatus;
    private final String code;
    private final String message;

}
