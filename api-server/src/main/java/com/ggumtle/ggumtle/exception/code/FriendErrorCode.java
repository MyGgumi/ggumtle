package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;


@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum FriendErrorCode implements ErrorCode {
    CANNOT_REQUEST_SELF("FR40001", "자기 자신에게는 친구 요청을 할 수 없습니다"),
    PARAMETER_REQUIRED("FR40002", "누락된 파라미터가 있습니다"),

    ALREADY_FRIEND("FR40901", "이미 친구 관계입니다"),
    ALREADY_REQUEST("FR40902", "이미 친구 요청을 보낸 상태입니다"),

    TARGET_NOT_FOUND("FR40401", "대상 회원을 찾을 수 없습니다"),
    REQUEST_NOT_FOUND("FR40402", "친구 요청을 찾을 수 없습니다"),
    FRIEND_NOT_FOUND("FR40403","친구 관계가 아닙니다"),

    DELETE_FRIEND_FORBIDDEN("FR40301", "삭제할 권한이 없습니다")
    ;

    private final String code;
    private final String message;

    public HttpStatus getHttpStatus() {
        return null;
    }
}
