package com.ggumtle.ggumtle.auth.presentation;

import com.ggumtle.ggumtle.auth.application.AuthService;
import com.ggumtle.ggumtle.auth.application.command.LoginCommand;
import com.ggumtle.ggumtle.auth.application.result.LoginResult;
import com.ggumtle.ggumtle.auth.presentation.request.LoginRequest;
import com.ggumtle.ggumtle.auth.presentation.response.LoginResponse;
import com.ggumtle.ggumtle.auth.presentation.response.LogoutResponse;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.AuthErrorCode;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import jakarta.validation.Valid;

@Controller
@RequestMapping("/auth")
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class AuthController {
    private final AuthService authService;

    @PostMapping("/login")
    public ResponseEntity<LoginResponse> login(@Valid @RequestBody LoginRequest request) {
        LoginCommand command = request.toCommand();
        LoginResult result = authService.login(command);
        return ResponseEntity.ok(LoginResponse.from(result));
    }

    @PostMapping("/logout")
    public ResponseEntity<LogoutResponse> logout(@RequestHeader("Authorization") String authorizationHeader){
        String accessToken = resolveToken(authorizationHeader);
        if (accessToken == null) {
            throw new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN);
        }

        authService.logout(accessToken);

        return ResponseEntity.ok(new LogoutResponse(true));
    }

    private String resolveToken(String authorizationHeader) {
        if (authorizationHeader != null && authorizationHeader.startsWith("Bearer")) {
            return authorizationHeader.substring(7);
        }
        return null;
    }
}
