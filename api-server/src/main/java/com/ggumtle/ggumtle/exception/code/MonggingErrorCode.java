package com.ggumtle.ggumtle.exception.code;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum MonggingErrorCode implements ErrorCode{
    ALREADY_MAX_LEVEL(HttpStatus.BAD_REQUEST, "MG40002", "이미 최대 레벨입니다"),

    NOT_OWNER(HttpStatus.BAD_REQUEST, "MG40101", "몽깅이의 주인이 아닙니다"),

    WITHDRAW_MONGGING(HttpStatus.FORBIDDEN,"MG40301","탈퇴한 회원의 몽깅이 입니다"),

    MONGGING_NOT_FOUND(HttpStatus.NOT_FOUND, "MG40401", "몽깅이를 찾을 수 없습니다"),
    ENHANCE_CONFIG_NOT_FOUND(HttpStatus.NOT_FOUND, "MG40402", "강화 확률을 찾을 수 없습니다"),
    ENHANCE_STATS_NOT_FOUND(HttpStatus.NOT_FOUND, "MG40403", "강화 스탯을 찾을 수 없습니다")
    ;

    private final HttpStatus httpStatus;
    private final String code;
    private final String message;
}
