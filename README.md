# 🌙 Ggumtle (꿈틀)

## 📋 프로젝트 개요

**Ggumtle**은 낮에는 AR로 캐릭터를 육성하고 밤에는 친구들과 함께 악몽을 이겨내는 멀티 호러 1:4 대전 게임입니다.

## 🏗️ 프로젝트 구조

```
S13P21D103/
├── 🎮 unity/           # Unity 게임 클라이언트
├── 📱 android/         # Android 앱
├── 🎯 game-server/     # 게임 서버
├── 🌐 api-server/      # API 서버
├── 🧪 dream-test/      # 테스트
└── 📦 exec/            # 배포 및 실행 스크립트
```

## 🎥 프로젝트 소개 영상

[![Video Label](http://img.youtube.com/vi/srlmeW2e8Lk/0.jpg)](https://youtu.be/srlmeW2e8Lk)

## 🎮 구현

### 1. 로비 화면

| 대기방 | 파티초대 | 클래스 변경 | 준비 |
|:---:|:---:|:---:|:---:|
| ![대기방](./assets/대기방.gif) | ![파티초대](./assets/파티_초대.gif) | ![클래스 변경](./assets/몽깅이_클래스_변경.gif) | ![준비](./assets/준비.gif) |

### 2. 강화 화면

| 강화 | 강화 성공 | 강화 실패 |
|:---:|:---:|:---:|
| ![강화](./assets/강화.gif) | ![강화 성공](./assets/강화_성공.gif) | ![강화 실패](./assets/강화_실패.gif) |

### 3. AR 미션 화면

| 먹이 주기 | 사진 찍기(실패) | 사진 찍기(성공) | 인사하기 |
|:---:|:---:|:---:|:---:|
| ![먹이 주기](./assets/몽깅이_미션_먹이주기.gif) | ![사진 찍기(실패)](./assets/몽깅이_미션_사진_실패.gif) | ![사진 찍기(성공)](./assets/몽깅이_미션_사진_성공.gif) | ![인사하기](./assets/몽깅이_미션_인사하기.gif) |

### 4. 게임 플레이 화면

| 꿈틀이 파기 | 꿈틀이 정화|
|:---:|:---:|
| ![꿈틀이 파기](./assets/땅파기.gif) | ![꿈틀이 정화](./assets/정화.gif) |
| 몽깅이 기절 | 몽깅이 부활 |
| ![몽깅이 기절](./assets/몽깅이_기절.gif) | ![몽깅이 부활](./assets/몽깅이_부활.gif) |
| 몽둥이 공포 |
| ![몽둥이 공포](./assets/몽둥이공포.gif) |

## 5. 몽깅이 애니메이션

| 이동 | 빛 주기 |
|:---:|:---:|
| ![이동](./assets/몽깅이동.gif) | ![빛 주기](./assets/몽깅진짜밥줘.gif) |
|상자 열기 | 기절 |
| ![상자 열기](./assets/몽깅상자열어.gif) | ![기절](./assets/몽깅기절.gif) |
| 힐 | 피격 |
| ![힐](./assets/몽깅힐받기.gif) | ![피격](./assets/몽깅피격.gif) |

## 6. 몽둥이 애니메이션

| 이동 | 공격 |
|:---:|:---:|
| ![이동](./assets/몽둥_이동.gif) | ![공격](./assets/몽둥_공격.gif) |
| 꿈틀이 심기 | 공포 |
| ![꿈틀이 심기](./assets/몽둥_꿈틀이심기.gif) | ![공포](./assets/몽둥_공포.gif) |

## 🛠️ 기술 스택

### Frontend
- **Unity**: C# + DotNetty
- **Android**: Kotlin + Jetpack Compose

### Backend
- **Game Server**: Spring Boot + Netty
- **API Server**: Spring Boot + WebSocket
- **Database**: MySQL + Redis
- **Authentication**: JWT + Google OAuth

### 개발 도구
- Gradle (빌드 도구)
- Docker (컨테이너화)
- Git (버전 관리)

## ⚙️ 설치 및 실행

### 필요 환경
- Java 21
- Unity 6.0
- MySQL 8.0
- Redis 8.2.1
- Docker
- Gradle 8.14.3

## 🎯 아키텍처

### 시스템 구성도

![시스템 구성도](./assets/시스템_구성도.png)

### 통신 모듈

![통신 모듈](./assets/통신_모듈.png)

### 안드로이드

![alt text](./assets/안드로이드.png)

### Unity

![alt text](./assets/유니티.png)

![alt text](./assets/유니티2.png)

## 📚 API 문서

[API 문서 링크](https://horse-ocicat-fc7.notion.site/API-25af12e1a08880a5bf2fed2aaf39102f?pvs=74)

## 🌳 브랜치 전략

- `master`: 배포 브랜치
- `develop`: 개발 브랜치
- `feature/*`: 기능 개발
- `fix/*`: 버그 수정

## 👥 팀 마이꿈이

| 팀장 | 팀원 | 팀원 | 팀원 | 팀원 | 팀원 |
| :---: | :---: | :---: | :---: | :---: | :---: |
| @ys05059 | @rhflffkaksl | @newsungk7 | @dhwm0710 | @kiryanchi | @wgwjh05169 |
| 최재웅 | 김수현 | 김성민 | 공연경 | 박기현 | 정현정 |
| Unity | Designer 및 Unity | Android 및 Unity | BE | BE | BE |