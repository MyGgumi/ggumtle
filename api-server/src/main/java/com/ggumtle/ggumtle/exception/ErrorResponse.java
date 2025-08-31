package com.ggumtle.ggumtle.exception;

public record ErrorResponse(
        boolean success,
        String code,
        String message
) {
    public static ErrorResponse fail(String code, String message) {
        return new ErrorResponse(false, code, message);
    }
}