package com.ggumtle.ggumtle.mongging.application;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MonggingErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.mongging.application.result.EnhanceMonggingResult;
import com.ggumtle.ggumtle.mongging.application.result.MonggingDetailResult;
import com.ggumtle.ggumtle.mongging.application.result.MonggingListResult;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import com.ggumtle.ggumtle.mongging.persistence.EnhancePercentageRepository;
import com.ggumtle.ggumtle.mongging.persistence.MonggingClassRepository;
import com.ggumtle.ggumtle.mongging.persistence.MonggingRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Slf4j
@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MonggingService {
    private final MonggingRepository monggingRepository;
    private final MonggingClassRepository monggingClassRepository;
    private final EnhancePercentageRepository enhancePercentageRepository;

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

        // 몽깅이 주인이 아니면 예외
        if (!mongging.getOwner().getId().equals(memberId)) {
            throw new GgumtleException(MonggingErrorCode.NOT_OWNER);
        }

        // 현재 레벨에 따른 강화 수치를 가져옴
        var currentEnhancePercentage = enhancePercentageRepository.findByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel())
                .orElseThrow(() -> new GgumtleException(MonggingErrorCode.ENHANCE_PERCENTAGE_NOT_FOUND));

        // 다음 레벨에 따른 강화 수치를 가져옴 (최대 레벨인 경우 없을 수 있음)
        var nextEnhancePercentageOpt = enhancePercentageRepository.findByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel() + 1);

        // 최대 레벨인지 확인
        if (nextEnhancePercentageOpt.isEmpty()) {
            return MonggingDetailResult.ofMaxLevel(mongging, currentEnhancePercentage);
        }

        return MonggingDetailResult.of(mongging, currentEnhancePercentage, nextEnhancePercentageOpt.get());
    }

    /**
     * 유저의 모든 몽깅이 조회
     * @param memberId 유저 아이디
     * @return 유저가 가지고 있는 몽깅이 목록
     */
    @Transactional(readOnly = true)
    public MonggingListResult fetchMonggings(Long memberId) {
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

        // 몽깅이 주인이 아니면 예외
        if (!mongging.getOwner().getId().equals(memberId)) {
            throw new GgumtleException(MonggingErrorCode.NOT_OWNER);
        }

        // 강화 확률 체크
        var enhancePercentage = enhancePercentageRepository.findByMonggingClassAndLevel(mongging.getMonggingClass(), mongging.getLevel())
            .orElseThrow(() -> new GgumtleException(MonggingErrorCode.ENHANCE_PERCENTAGE_NOT_FOUND));

        // 강화 확률대로 몽깅이 강화
        boolean isSuccess = mongging.enhance(enhancePercentage);

        // 성공 및 실패
        if (isSuccess) {
            return EnhanceMonggingResult.ofSuccess(mongging, enhancePercentage);
        } else {
            return EnhanceMonggingResult.ofFail(mongging, enhancePercentage);
        }
    }
}
