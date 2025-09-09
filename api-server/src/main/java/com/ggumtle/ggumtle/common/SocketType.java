package com.ggumtle.ggumtle.common;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.CommonErrorCode;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;

@AllArgsConstructor(access = AccessLevel.PRIVATE)
@Getter
public enum SocketType {
    // 오류 응답
    UNKNOWN(null, "UNKNOWN"),

    // Auth
    CONNECT("CONNECT", "CONNECT"),

    // PARTY
    CREATE_PARTY("CREATE_PARTY", "CREATE_PARTY_RESULT"),
    INVITE_PARTY("INVITE_PARTY", "INVITE_PARTY_RESULT"),
    ACCEPT_PARTY_INVITATION("ACCEPT_PARTY_INVITATION", "ACCEPT_PARTY_INVITATION_RESULT"),
    LEAVE_PARTY("LEAVE_PARTY", "LEAVE_PARTY_RESULT"),

    // DREAM
    START_DREAM("START_DREAM", "START_DREAM_RESULT"),
    READY_DREAM("READY_DREAM", "READY_DREAM_RESULT"),
    UNREADY_DREAM("UNREADY_DREAM", "UNREADY_DREAM_RESULT"),
    MATCHING_CANCELLED("MATCHING_CANCELLED", "MATCHING_CANCELLED_RESULT"),

    // FRIEND
    REQUEST_FRIEND("REQUEST_FRIEND", "REQUEST_FRIEND_RESULT"),
    GET_FRIENDS("GET_FRIENDS", "GET_FRIENDS"),
    GET_FRIEND_REQUESTS("GET_FRIEND_REQUESTS", "GET_FRIEND_REQUESTS_RESULT"),
    ACCEPT_FRIEND_REQUEST("ACCEPT_FRIEND_REQUEST", "ACCEPT_FRIEND_REQUEST_RESULT"),
    REJECT_FRIEND_REQUEST("REJECT_FRIEND_REQUEST", "REJECT_FRIEND_REQUEST_RESULT"),
    SEARCH_MEMBER("SEARCH_MEMBER", "SEARCH_MEMBER_RESULT"),
    ;

    private final String requestType;
    private final String responseType;

    public static SocketType valueOfRequest(String requestType) {
        for (SocketType type : SocketType.values()) {
            if (type.requestType != null && type.requestType.equals(requestType)) {
                return type;
            }
        }

        throw new GgumtleException(CommonErrorCode.BAD_REQUEST, requestType + "에 해당하는 SocketType을 찾을 수 없습니다");
    }
}
