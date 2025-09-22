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
        name = "enhance_stat",
        uniqueConstraints = @UniqueConstraint(name = "uk_enhance_stats", columnNames = {"level", "mongging_class_id"})
)
public class EnhanceStat {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "level")
    private Integer level;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "mongging_class_id", nullable = false)
    private MonggingClass monggingClass;

    @Column(name = "enhance_percentage", nullable = false)
    private Double enhancePercentage;
}
