package com.ggumtle.ggumtle.exception;

public class GgumtleException extends RuntimeException {
    private final ErrorCode errorCode;

    public GgumtleException(ErrorCode errorCode) {
        super(errorCode.getMessage());
        this.errorCode = errorCode;
    }

    public ErrorCode getErrorCode() {
        return errorCode;
    }
}