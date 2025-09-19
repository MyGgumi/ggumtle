package com.ggumtle.ggumtle.member.domain;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MemberErrorCode;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import lombok.AccessLevel;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.ToString;

import java.time.LocalDate;
import java.time.LocalDateTime;

@Entity
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Getter
@ToString
public class Member {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "member_id")
    private Long id;

    @Column(nullable = false, unique = true)
    private String googleEmail;

    @Column(nullable = false)
    private String name;

    @Column(nullable = false, unique = true)
    private String nickname;

    private LocalDate birthDate;

    @Column(nullable = false)
    private LocalDateTime signUpAt;

    private Boolean isDeleted;

    private Integer coin;

    @Builder
    public Member(String googleEmail, String name, String nickname, LocalDate birthDate) {
        this.googleEmail = googleEmail;
        this.name = name;
        this.nickname = nickname;
        this.birthDate = birthDate;
        this.signUpAt = LocalDateTime.now();
        this.isDeleted = false;
        this.coin = 0;
    }

    public void spend(int coin) {
        if (this.coin < coin) {
            throw new GgumtleException(MemberErrorCode.NOT_ENOUGH_COIN);
        }

        this.coin -= coin;
    }

    public void increaseCoinCappedToMax(int amount) {
        long result = (long) this.coin + amount;

        if (result > Integer.MAX_VALUE) {
            this.coin = Integer.MAX_VALUE;
        } else {
            this.coin = (int) result;
        }
    }
}
