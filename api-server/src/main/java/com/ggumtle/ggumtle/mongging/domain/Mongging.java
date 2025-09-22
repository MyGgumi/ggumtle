package com.ggumtle.ggumtle.mongging.domain;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MonggingErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Entity
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Getter
public class Mongging {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "owner_id", nullable = false)
    private Member owner;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "class_id", nullable = false)
    private MonggingClass monggingClass;

    @Column(name = "level", nullable = false)
    private Integer level;

    @Column(nullable = false)
    private Boolean isDeleted;

    public Mongging (Member owner, MonggingClass monggingClass) {
        this.owner = owner;
        this.monggingClass = monggingClass;
        this.level = 1;
        this.isDeleted = false;
    }

    public boolean enhance(EnhanceConfig enhanceConfig) {
        if (level >= 5) {
            throw new GgumtleException(MonggingErrorCode.ALREADY_MAX_LEVEL);
        }

        owner.spend(enhanceConfig.getRequiredCoin());

        if (Math.random() < enhanceConfig.getSuccessPercentage()) {
            level++;
            return true;
        } else {
            return false;
        }
    }

    public void deleteMongging() {
        this.isDeleted = true;
    }
}
