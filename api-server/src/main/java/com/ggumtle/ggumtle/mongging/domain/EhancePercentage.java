package com.ggumtle.ggumtle.mongging.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.MapsId;
import jakarta.persistence.OneToOne;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Entity
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Getter
public class EhancePercentage {
    @Id
    private Long id;

    @OneToOne(fetch = FetchType.LAZY)
    @MapsId
    @JoinColumn(name = "mongging_class")
    private MonggingClass monggingClass;

    @Column(nullable = false)
    private int level;

    @Column(nullable = false)
    private Double percentage;

    @Column(nullable = false)
    private int requiredCoin;

    @Column(nullable = false)
    private Double successPercentage;
}
