package com.ggumtle.ggumtle.mongging.application;

import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
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

    @Transactional
    public void createMongging(Member newMember){
        List<MonggingClass> allClass = monggingClassRepository.findAll();

        List<Mongging> monggings = new ArrayList<>();
        for(MonggingClass monggingClass : allClass){
            monggings.add(Mongging.createMongging(newMember, monggingClass));
        }
        monggingRepository.saveAll(monggings);
    }
}
