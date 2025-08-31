package com.ggumtle.ggumtle.exception.errorCode;

import com.ggumtle.ggumtle.exception.ErrorCode;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum AuthErrorCode implements ErrorCode {
    INVALID_ID_TOKEN(HttpStatus.BAD_REQUEST, "AU40001", "유효하지 않은 ID 토큰입니다"),
    ALREADY_SIGNED_UP(HttpStatus.CONFLICT,   "AU40901", "이미 가입된 유저입니다"),
    NICKNAME_DUPLICATED(HttpStatus.CONFLICT, "AU40902", "이미 사용 중인 닉네임입니다"),
    NEED_SIGNUP(HttpStatus.CONFLICT, "AU40903", "회원가입이 필요합니다");

    private final HttpStatus httpStatus;
    private final String code;
    private final String message;
}
