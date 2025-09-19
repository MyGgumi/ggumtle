package com.ggumtle.ggumtle.mongging.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Entity
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Getter
@Table(
        name = "enhance_percentage",
        uniqueConstraints = @UniqueConstraint(name = "uk_enhance_percentage", columnNames = {"level", "mongging_class_id"})
)
public class EnhancePercentage {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "level")
    private Integer level;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "mongging_class_id", nullable = false)
    private MonggingClass monggingClass;

    @Column(name = "current_percentage", nullable = false)
    private Double currentPercentage;

    @Column(name = "next_percentage", nullable = false)
    private Double nextPercentage;

    @Column(name = "required_coin", nullable = false)
    private Integer requiredCoin;

    @Column(name = "success_percentage", nullable = false)
    private Double successPercentage;
}
