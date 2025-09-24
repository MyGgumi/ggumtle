package com.ggumtle.ggumtle.mongging.application;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MemberErrorCode;
import com.ggumtle.ggumtle.exception.code.MonggingErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import com.ggumtle.ggumtle.mongging.application.result.EnhanceMonggingResult;
import com.ggumtle.ggumtle.mongging.application.result.MonggingDetailResult;
import com.ggumtle.ggumtle.mongging.application.result.MonggingListResult;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import com.ggumtle.ggumtle.mongging.persistence.EnhanceConfigRepository;
import com.ggumtle.ggumtle.mongging.persistence.EnhanceStatRepository;
import com.ggumtle.ggumtle.mongging.persistence.MonggingClassRepository;
import com.ggumtle.ggumtle.mongging.persistence.MonggingRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

@Slf4j
@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MonggingService {
    private final MonggingRepository monggingRepository;
    private final MonggingClassRepository monggingClassRepository;
    private final EnhanceStatRepository enhanceStatRepository;
    private final EnhanceConfigRepository enhanceConfigRepository;
    private final MemberRepository memberRepository;

    /**
     * 몽깅이 상세 조회
     * @param memberId 멤버 아이디
     * @param monggingId 몽깅이 아이디
     * @return 몽깅이 상세 정보
     */
    @Transactional(readOnly = true)
    public MonggingDetailResult fetchMonggingDetail(Long memberId, Long monggingId) {
        // 몽깅이를 조회함
        var mongging = monggingRepository.findById(monggingId)
                .orElseThrow(() -> new GgumtleException(MonggingErrorCode.MONGGING_NOT_FOUND));

        // isDeleted = true 라면 탈퇴한 회원의 몽깅이이므로 예외
        if (mongging.getIsDeleted() == Boolean.TRUE){
            throw new GgumtleException(MonggingErrorCode.WITHDRAW_MONGGING);
        }

        // 몽깅이 주인이 아니면 예외
        if (!mongging.getOwner().getId().equals(memberId)) {
            throw new GgumtleException(MonggingErrorCode.NOT_OWNER);
        }

        // 현재 레벨과 다음 레벨 강화 수치를 한 번에 가져옴 (Top-2 쿼리)
        var enhanceStats = enhanceStatRepository.findTop2ByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel());
        
        // 1개 이상의 결과가 없다면 비정상적인 상황
        if (enhanceStats.isEmpty()) {
            throw new GgumtleException(MonggingErrorCode.ENHANCE_STATS_NOT_FOUND);
        }

        // 1개 결과는 최대 레벨
        if (enhanceStats.size() == 1) {
            return MonggingDetailResult.ofMaxLevel(mongging, enhanceStats);
        }

        // 2개 결과는 다음 레벨이 존재하므로 강화 확률을 가져옴
        var nextConfig = enhanceConfigRepository.findByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel() + 1)
                .orElseThrow(() -> new GgumtleException(MonggingErrorCode.ENHANCE_CONFIG_NOT_FOUND));

        return MonggingDetailResult.of(mongging, enhanceStats, nextConfig);
    }

    /**
     * 유저의 모든 몽깅이 조회
     * @param memberId 유저 아이디
     * @return 유저가 가지고 있는 몽깅이 목록
     */
    @Transactional(readOnly = true)
    public MonggingListResult fetchMonggings(Long memberId) {
        Member owner = memberRepository.findById(memberId)
                .orElseThrow(() -> new GgumtleException(MemberErrorCode.NOT_FOUND));

        if (owner.getIsDeleted() == Boolean.TRUE) {
            throw new GgumtleException(MemberErrorCode.WITHDRAW_MEMBER);
        }

        List<Mongging> monggings = monggingRepository.findAllByOwnerIdFetchClassAndOwner(memberId);

        return MonggingListResult.of(monggings);
    }

    @Transactional
    public void createMongging(Member newMember){
        List<MonggingClass> allClass = monggingClassRepository.findAll();

        List<Mongging> monggings = new ArrayList<>();
        for(MonggingClass monggingClass : allClass){
            monggings.add(new Mongging (newMember, monggingClass));
        }
        monggingRepository.saveAll(monggings);
    }

    @Transactional
    public EnhanceMonggingResult enhanceMongging(Long memberId, Long monggingId){
        var mongging = monggingRepository.findById(monggingId)
            .orElseThrow(() -> new GgumtleException(MonggingErrorCode.MONGGING_NOT_FOUND));

        // isDeleted = true 라면 탈퇴한 회원의 몽깅이이므로 예외
        if (mongging.getIsDeleted() == Boolean.TRUE){
            throw new GgumtleException(MonggingErrorCode.WITHDRAW_MONGGING);
        }

        // 몽깅이 주인이 아니면 예외
        if (!mongging.getOwner().getId().equals(memberId)) {
            throw new GgumtleException(MonggingErrorCode.NOT_OWNER);
        }

        // 강화 설정 정보 가져옴
        var enhanceConfig = enhanceConfigRepository.findByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel() + 1)
            .orElseThrow(() -> new GgumtleException(MonggingErrorCode.ENHANCE_CONFIG_NOT_FOUND));

        // 현재 레벨 강화 수치 정보 가져옴
        var enhanceStats = enhanceStatRepository.findTop2ByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel());

        // 1개 이상의 결과가 없다면 비정상적인 상황
        if (enhanceStats.isEmpty()) {
            throw new GgumtleException(MonggingErrorCode.ENHANCE_STATS_NOT_FOUND);
        }

        // 강화 확률대로 몽깅이 강화
        boolean isSuccess = mongging.enhance(enhanceConfig);

        // 성공 및 실패
        if (isSuccess) {
            var afterEnhanceConfig = enhanceConfigRepository.findByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel() + 1)
                    .orElseThrow(() -> new GgumtleException(MonggingErrorCode.ENHANCE_CONFIG_NOT_FOUND));
            return EnhanceMonggingResult.ofSuccess(mongging, enhanceStats, afterEnhanceConfig);
        } else {
            return EnhanceMonggingResult.ofFail(mongging, enhanceStats, enhanceConfig);
        }
    }
}
