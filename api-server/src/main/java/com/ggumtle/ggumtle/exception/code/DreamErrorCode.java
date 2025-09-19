package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum DreamErrorCode implements ErrorCode {
    FORBIDDEN_CANCEL_MATCHING(HttpStatus.FORBIDDEN, "DR40301", "매칭을 취소할 권한이 없습니다"),
    NOT_MY_INVITATION(HttpStatus.FORBIDDEN, "DR40302", "자신이 받은 초대가 아닙니다"),
    NOT_LEADER(HttpStatus.FORBIDDEN, "DR40303", "파티의 리더가 아닙니다"),
    NOT_OWNER_OF_MONGGING(HttpStatus.FORBIDDEN, "DR40304", "몽깅이의 주인이 아닙니다"),

    NOT_FOUND_PARTY(HttpStatus.NOT_FOUND, "DR40401", "파티를 찾을 수 없습니다"),
    NOT_FOUND_INVITATION(HttpStatus.NOT_FOUND, "DR40402", "파티 초대를 찾을 수 없습니다"),
    NOT_FOUND_MATCHING(HttpStatus.NOT_FOUND, "DR40403", "매칭 대기 상태가 아니거나 이미 처리된 요청입니다"),
    NOT_FOUND_DREAM(HttpStatus.NOT_FOUND, "DR40404", "드림을 찾을 수 없습니다"),
    NOT_FOUND_MONGGING(HttpStatus.NOT_FOUND, "DR40405", "몽깅이를 찾을 수 없습니다"),

    NOT_ALL_READY(HttpStatus.CONFLICT, "DR40901", "아직 준비하지 않은 파티원이 존재합니다"),
    ALREADY_IN_PARTY(HttpStatus.CONFLICT, "DR40902", "이미 파티에 속해 있습니다"),
    CANNOT_LEAVE_WHILE_MATCHING(HttpStatus.CONFLICT, "DR40903", "드림 매칭 대기 중에는 파티를 나갈 수 없습니다"),
    ALREADY_MATCHING(HttpStatus.CONFLICT, "DR40904", "이미 드림 매칭 중입니다"),
    ALREADY_INVITED_USER(HttpStatus.CONFLICT, "DR40905", "이미 초대한 멤버입니다"),
    ;

    private final HttpStatus httpStatus;
    private final String code;
    private final String message;

}
