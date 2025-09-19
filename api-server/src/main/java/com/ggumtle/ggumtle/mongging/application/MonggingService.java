package com.ggumtle.ggumtle.mongging.application;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MonggingErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.mongging.application.result.EnhanceMonggingResult;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import com.ggumtle.ggumtle.mongging.persistence.EnhancePercentageRepository;
import com.ggumtle.ggumtle.mongging.persistence.MonggingClassRepository;
import com.ggumtle.ggumtle.mongging.persistence.MonggingRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MonggingService {
    private final MonggingRepository monggingRepository;
    private final MonggingClassRepository monggingClassRepository;
    private final EnhancePercentageRepository enhancePercentageRepository;

    @Transactional
    public void createMongging(Member newMember){
        List<MonggingClass> allClass = monggingClassRepository.findAll();

        List<Mongging> monggings = new ArrayList<>();
        for(MonggingClass monggingClass : allClass){
            monggings.add(Mongging.createMongging(newMember, monggingClass));
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
