package com.ggumtle.ggumtle.exception;

import com.ggumtle.ggumtle.exception.code.ErrorCode;
import org.springframework.http.HttpStatus;

public class GgumtleException extends RuntimeException {
    private final ErrorCode errorCode;
    private final String message;

    public GgumtleException(ErrorCode errorCode) {
        super(errorCode.getMessage());
        this.errorCode = errorCode;
        this.message = errorCode.getMessage();
    }

    public GgumtleException(ErrorCode errorCode, String message) {
        super(errorCode.getMessage());
        this.errorCode = errorCode;
        this.message = message;
    }

    public String getCode() {
        return errorCode.getCode();
    }

    public String getMessage() {
        return message;
    }

    public HttpStatus getHttpStatus() {
        return errorCode.getHttpStatus();
    }
}