package com.ggumtle.ggumtle.exception;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(GgumtleException.class)
    public ResponseEntity handleGgumtleException(GgumtleException e) {
        return ResponseEntity.status(e.getHttpStatus()).body(ErrorResponse.fail(e.getCode(), e.getMessage()));
    }
}
