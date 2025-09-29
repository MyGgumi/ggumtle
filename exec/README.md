# 마이꿈이 포팅 매뉴얼

## 1. Gitlab 소스 클론 이후 빌드 및 배포할 수 있도록 정리한 문서

## 서버 환경

```text
- Java 21
- Spring Boot 3.5.5
- Gradle 8.14.3
- MySql 8.0
- Redis 8.2.1
```

`꿈틀`의 서버는 API 서버(아웃게임)와 Game 서버(인게임)로 나뉘어져 있습니다.

### 0. 들어가기 전

- Docker 및 Docker Compose 설치
- EC2 인스턴스 1대, 게임 서버 PC 1대 사용
- MySql, Redis, Prometheus, Loki, Grafana는 EC2에 설치되어 있음
- MySql은 게임 서버 PC에 설치되어 있음

#### 배포 시 특이 사항

> 인게임을 위한 MySql과 아웃게임을 위한 MySql이 분리되어 있습니다.


### 1. API 서버 (api-server)

1. `/api-server/.env` 파일 작성

    ```properties
    # Google OAuth 설정
    GOOGLE_CLIENT_ID=639420014561-3799jp33go41furqju0himljgmi5ehil.apps.googleusercontent.com
    GOOGLE_CLIENT_SECRET=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

    # JWT 설정
    ACCESS_EXPIRE=10800000
    ACCESS_SECRET=kXb2WnXqYl0t6w3sC0YkqZlE6uLhIsc+E8P0oF2u5ho=
    ACCESS_NAME=access_token

    # Prometheus (EC2에 배포 되어있음)
    PROMETHEUS=http://j13d103.p.ssafy.io:9090/api/v1/query

    # MySql 설정 (EC2에 배포 되어있음)
    MYSQL_URL=jdbc:mysql://j13d103.p.ssafy.io:3306/ggumtle
    MYSQL_USERNAME=root
    MYSQL_PASSWORD=myd103admin!

    # Redis 설정 (EC2에 배포 되어있음)
    REDIS_HOST=j13d103.p.ssafy.io
    REDIS_PORT=6379
    REDIS_PASSWORD=d103admin!
    ```

2. `gradle` 실행

    ```bash
    chmod +x ./gradlew
    ./gradlew bootRun
    ```

### 2. Game 서버 (game-server)

1. `game-server/.env` 파일 작성

    ```properties
    # JWT 설정
    ACCESS_SECRET=kXb2WnXqYl0t6w3sC0YkqZlE6uLhIsc+E8P0oF2u5ho=

    # Timezone 설정
    TIMEZONE=Asia/Seoul

    # Encoding 설정
    ENCODING=UTF-8

    # 서버별 포트 설정
    GAME_SERVER_PORT=8888
    METRIC_SERVER_PORT=8889
    GAME_SERVER_PORT_2=8898
    METRIC_SERVER_PORT_2=8899
    GAME_SERVER_PORT_3=8878
    METRIC_SERVER_PORT_3=8879

    # Redis 설정 (EC2에 배포 되어있음)
    REDIS_HOST=j13d103.p.ssafy.io
    REDIS_PORT=6379
    REDIS_PASSWORD=yKzGAxcPTPXPrhayZJ3T

    # MySql 설정 (게임 서버 PC에 배포 되어있음)
    MYSQL_HOST=p-ryan.iptime.org
    MYSQL_USERNAME=root
    MYSQL_PASSWORD=superWkdWKdTpsPassqjsgh23!@
    MYSQL_PORT=3306
    MYSQL_DATABASE=ggumtle
    MYSQL_USE_SSL=false
    MYSQL_ALLOW_PUBLIC_KEY_RETRIEVAL=true
    ```

2. `gradle` 명령어 실행

    ```bash
    chmod +x ./gradlew
    ./gradlew bootRun
    ```

## 3. 안드로이드 (Unity)

## **개발 환경 요구사항**

**Android Studio:**

- AGP 8.9.0 → **Android Studio Ladybug (2024.2.1) 이상** 필요

**JDK:**

- Kotlin 2.1.10 + AGP 8.9.0 → **JDK 17 이상** 필요

**최소 지원:**

- minSdk 24 (Android 7.0)
- compileSdk 35 (Android 15)

## **외부 서비스 설정 필요**

**Google 서비스:**

- `androidx-credentials-play-services-auth`
- `google-identity-googleid`

## 2. 프로젝트에서 사용하는 외부 서비스 정보를 정리한 문서

### 소셜 인증

- Google OAuth 2.0
    - Client ID: 639420014561-3799jp33go41furqju0himljgmi5ehil.apps.googleusercontent.com
    - Client Secret: GOCSPX-Xmq1lTWLOc2cqz

## 3. 시연 시나리오

```
아웃게임
1. 로그인 완료 상태에서 대기방 이동
2. 대기방에서 성장 이동
3. 성장에서 ar로 이동 후데일리 미션 수헹
4. 데일리 미션 수행 후 보상받기
5. 강화 진행
6. 다시 대기방이동후 게임시작
7. 로딩화면보여주고 끝

인게임
1. 입장상태에서 상자템파밍 시작
2. 스피드 템 사용하여 빠르게 파밍 (테이저건-> 제세동기 순)
2. 파밍도중 몽둥이 마주침
3. 몽둥이가 공포걸기
4. 공포로 검은화면이 끝나면 몽둥이가 몽깅이 바로앞에있음
5. 몽둥이가 때려서 기절
6. 몽둥이는 딴데로 이동후 재새동기로 부활  -> x  다른 몽깅이가 부활시켜주기
7. 몽둥이한테 테이저건 사용 
8. 꿈틀이 팟는데 가짜꿈틀이
9. 다른 꿈틀이 파기
10. 꿈틀이 정화
11. 탈출구를 향해 달리기
12. 다른플레이어가 먼저 탈출
13. 탈출후 결과화면
```
