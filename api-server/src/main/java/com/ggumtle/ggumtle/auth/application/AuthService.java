package com.ggumtle.ggumtle.auth.application;

import com.ggumtle.ggumtle.auth.application.command.LoginCommand;
import com.ggumtle.ggumtle.auth.application.result.LoginResult;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.AuthErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import com.ggumtle.ggumtle.auth.jwt.JwtProvider;
import com.ggumtle.ggumtle.mission.application.MissionService;
import com.ggumtle.ggumtle.mongging.application.MonggingService;
import com.google.api.client.googleapis.auth.oauth2.GoogleIdToken.Payload;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.concurrent.ThreadLocalRandom;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class AuthService {

    private final OAuthClient oAuthClient;
    private final MemberRepository memberRepository;
    private final JwtProvider jwtProvider;
    private final MonggingService monggingService;
    private final MissionService missionService;

    /**
     * 로그인 + 자동 회원가입 (통합)
     * - Google idToken 검증
     * - 기존 회원 조회: googleEmail 기준
     * - 없으면 자동 생성(닉네임 = "user" + 4자리 숫자, 중복 시 재시도)
     * - JWT 발급(subject = memberId)
     */
    @Transactional
    public LoginResult login(LoginCommand command) {
        Payload payload = oAuthClient.verify(command.idToken())
                .orElseThrow(() -> new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN));

        String googleEmail = payload.getEmail();
        String googleName  = (String) payload.get("name");

        if (googleEmail == null || googleEmail.isBlank()) {
            throw new GgumtleException(AuthErrorCode.INVALID_ID_TOKEN);
        }

        Member member = memberRepository.findByGoogleEmail(googleEmail).orElse(null);

        if (member == null) {
            String name = firstNonBlank(googleName, emailLocalPart(googleEmail));
            String nickname = generateNickname();

            member = Member.builder()
                    .googleEmail(googleEmail)
                    .name(name)
                    .nickname(nickname)
                    .build();

            try {
                memberRepository.save(member);
                monggingService.createMongging(member);
                missionService.createMemberMission(member);
            } catch (DataIntegrityViolationException e) {
                Member existing = memberRepository.findByGoogleEmail(googleEmail).orElse(null);
                if (existing == null) throw e;
                member = existing;
            }
        }
        String accessToken = jwtProvider.issueAccessToken(member.getId());
        return new LoginResult(member.getId(), accessToken);
    }

    private String generateNickname() {
        String candidate;
        do {
            int rand = ThreadLocalRandom.current().nextInt(1000, 10000);
            candidate = "user" + rand;
        } while (memberRepository.existsByNickname(candidate));
        return candidate;
    }

    private static String firstNonBlank(String... candidates) {
        for (String c : candidates) {
            if (c != null && !c.isBlank()) return c.trim();
        }
        return null;
    }

    private static String emailLocalPart(String email) {
        int idx = email.indexOf('@');
        return (idx > 0) ? email.substring(0, idx) : email;
    }
}
