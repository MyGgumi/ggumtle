package com.ggumtle.ggumtle.auth;

import com.ggumtle.ggumtle.auth.application.AuthService;
import com.ggumtle.ggumtle.auth.application.command.LoginCommand;
import com.ggumtle.ggumtle.auth.application.command.SignUpCommand;
import com.ggumtle.ggumtle.auth.application.result.LoginResult;
import com.ggumtle.ggumtle.auth.application.result.SignUpResult;
import com.ggumtle.ggumtle.auth.presentation.request.LoginRequest;
import com.ggumtle.ggumtle.auth.presentation.request.SignUpRequest;
import com.ggumtle.ggumtle.auth.presentation.response.LoginResponse;
import com.ggumtle.ggumtle.auth.presentation.response.SignUpResponse;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
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

    @PostMapping("/signup")
    public ResponseEntity<SignUpResponse> signUp(@Valid @RequestBody SignUpRequest request) {
        SignUpCommand command = request.toCommand();
        SignUpResult result = authService.signUp(command);
        return ResponseEntity.ok(SignUpResponse.from(result));
    }
}
