package com.ggumtle.ggumtle.auth.application;

import com.ggumtle.ggumtle.auth.application.command.LoginCommand;
import com.ggumtle.ggumtle.auth.application.command.SignUpCommand;
import com.ggumtle.ggumtle.auth.application.result.LoginResult;
import com.ggumtle.ggumtle.auth.application.result.SignUpResult;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import com.ggumtle.ggumtle.security.jwt.JwtProvider;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.errorCode.AuthErrorCode;
import com.google.api.client.googleapis.auth.oauth2.GoogleIdToken.Payload;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class AuthService {

    private final OAuthClient oAuthClient;
    private final MemberRepository memberRepository;
    private final JwtProvider jwtProvider;

    @Transactional(readOnly = true)
    public LoginResult login(LoginCommand command) {
        Payload payload = oAuthClient.verify(command.idToken())
                .orElseThrow(() -> new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN));

        String email = payload.getEmail();
        if (email == null || email.isBlank()) {
            throw new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN);
        }

        Member member = memberRepository.findByGoogleEmail(email)
                .orElseThrow(() -> new GgumtleException(AuthErrorCode.NEED_SIGNUP));

        String accessToken = jwtProvider.issueAccessToken(member.getId());
        return new LoginResult(member.getId(), accessToken);
    }

    @Transactional
    public SignUpResult signUp(SignUpCommand command) {
        Payload payload = oAuthClient.verify(command.idToken())
                .orElseThrow(() -> new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN));
        String email = payload.getEmail();
        if (email == null || email.isBlank()) {
            throw new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN);
        }

        if (memberRepository.existsByGoogleEmail(email)) {
            throw new GgumtleException(AuthErrorCode.ALREADY_SIGNED_UP);
        }

        if (memberRepository.existsByNickname(command.nickname())) {
            throw new GgumtleException(AuthErrorCode.NICKNAME_DUPLICATED);
        }

        Member member = Member.builder()
                .googleEmail(email)
                .name(command.name())
                .nickname(command.nickname())
                .birthDate(command.birthDate())
                .build();

        memberRepository.save(member);

        String accessToken = jwtProvider.issueAccessToken(member.getId());
        return new SignUpResult(member.getId(), accessToken);
    }
}
