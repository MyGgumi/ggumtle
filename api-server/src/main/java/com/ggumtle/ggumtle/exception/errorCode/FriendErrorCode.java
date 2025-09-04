package com.ggumtle.ggumtle.exception.errorCode;

import com.ggumtle.ggumtle.exception.ErrorCode;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum FriendErrorCode implements ErrorCode {
    CANNOT_REQUEST_SELF(HttpStatus.BAD_REQUEST, "FR40001", "자기 자신에게는 친구 요청을 할 수 없습니다"),
    TARGET_REQUIRED(HttpStatus.BAD_REQUEST, "FR40002", "친구 요청 memberId는 필수 입력값입니다."),

    ALREADY_FRIEND(HttpStatus.CONFLICT, "FR40901", "이미 친구 관계입니다"),
    ALREADY_REQUEST(HttpStatus.CONFLICT, "FR40902", "이미 친구 요청을 보낸 상태입니다."),
    REJECTED_REQUEST(HttpStatus.CONFLICT, "FR40903", "이미 거절한 친구 요청입니다."),

    TARGET_NOT_FOUND(HttpStatus.NOT_FOUND, "FR40401", "대상 회원을 찾을 수 없습니다"),
    REQUEST_NOT_FOUND(HttpStatus.NOT_FOUND, "FR40402", "친구 요청을 찾을 수 없습니다.");

    private final HttpStatus httpStatus;
    private final String code;
    private final String message;
}
