# Game Load Test - 프로젝트 컨텍스트

## 프로젝트 목적

Netty 기반 게임 서버(game-server)의 부하 테스트를 위한 클라이언트 도구

## 기술 스택

- Kotlin 2.0.21 + Coroutines 1.9.0
- Netty Client 4.2.x
- Clikt (CLI), kaml (YAML)
- kotlin-logging + Logback
- **Required Java**: 21 (Java 25는 Gradle 호환성 문제로 빌드 불가)

## 프로토콜 정보

### 패킷 헤더 (14 bytes, Big Endian)

| Offset | Type | Description |
|--------|------|-------------|
| 0-1 | short | Packet Type |
| 2-5 | int | Data Length |
| 6-13 | long | Timestamp |

### 주요 패킷 (Client → Server)

| Type | ID | Body |
|------|-----|------|
| VERIFY_TOKEN | 1 | UTF-8 string |
| ROOM_JOIN | 10 | Long (roomId) |
| SCENE_CHANGE | 30 | 없음 |
| PLAYER_MOVE | 40 | 3 ints (x, y, z) |
| SHOW_BOX | 50 | Int (boxId) |
| CLOSE_BOX | 52 | Int (boxId) |
| TAKE_ITEM_FROM_BOX | 54 | 2 ints (boxId, index) |
| PUT_ITEM_TO_BOX | 56 | 2 ints (boxId, itemId) |
| HIT_MONGGING | 60 | 3 ints + Long (vx, vy, vz, targetId) |
| START_REVIVE | 62 | Long (targetId) |
| STOP_REVIVE | 65 | 없음 |
| MONGDUNG_SKILL | 67 | Int (skillType) |
| ATTACK_WITH_ITEM | 69 | 4 ints (x, y, z, itemId) |
| USE_FIELD_ITEM | 71 | Int (itemId) |
| USE_DEFIBRILLATOR | 73 | 없음 |
| DIG_UP_GGUMTLE | 100 | Int (ggumtleId) |
| STOP_DIGGING | 102 | 없음 |
| START_FEED | 110 | Int (ggumtleId) |
| STOP_FEED | 112 | 없음 |
| ESCAPE | 140 | Int (exitId) |

### 주요 패킷 (Server → Client)

| Type | ID | Description |
|------|-----|-------------|
| VERIFY_TOKEN | 2 | 인증 결과 |
| ROOM_JOIN | 11 | 방 입장 결과 |
| INITIALIZE_MAP | 20 | 맵 초기화 데이터 |
| INITIALIZE_PLAYER | 21 | 플레이어 초기화 데이터 |
| SCENE_CHANGE | 31 | 씬 변경 결과 |
| GAME_START | 35 | 게임 시작 신호 |
| PLAYER_MOVE_RELAY | 41 | 이동 브로드캐스트 |
| END | 200 | 게임 종료 |

## 구현 현황

### 완료

- [x] 프로젝트 세팅 (build.gradle.kts, 디렉토리)
- [x] 프로토콜 레이어 (PacketType, Header, Codec)
- [x] 네트워크 레이어 (GameClient, ConnectionPool)
- [x] 플레이어 시뮬레이션 (VirtualPlayer)
- [x] 시나리오 DSL
- [x] CLI 인터페이스

### 현재 작업

Phase 1-2 구현 완료

### 다음 작업

- 테스트 및 디버깅 (Java 21 환경 필요)
- 추가 시나리오 구현
- 메트릭 대시보드 (선택사항)

### 빌드 참고사항

Java 21을 사용해야 빌드됩니다. Java 25는 Gradle 호환성 문제가 있습니다.

```bash
# Java 21 설치 후
export JAVA_HOME=/path/to/java21
./gradlew build
```

## 참조 파일

| 파일 | 설명 |
|------|------|
| `game-server/.../packet/PacketHeader.java` | 14-byte 헤더 형식 |
| `game-server/.../packet/ReceivePacketType.java` | 수신 패킷 타입 정의 |
| `dream-test/.../test/Client.java` | 패킷 전송 참조 구현 |
| `game-server/.../init/RoomCreator.java` | 테스트 룸 정보 (ID: -1~-20) |

## 프로젝트 구조

```
game-load-test/
├── build.gradle.kts
├── settings.gradle.kts
├── CLAUDE.md
└── src/main/kotlin/com/ggumtle/loadtest/
    ├── Application.kt
    ├── cli/
    │   ├── LoadTestCommand.kt
    │   └── RunCommand.kt
    ├── config/
    │   ├── LoadTestConfig.kt
    │   └── ConfigLoader.kt
    ├── protocol/
    │   ├── PacketType.kt
    │   ├── Packet.kt
    │   ├── PacketHeader.kt
    │   ├── codec/
    │   │   ├── PacketEncoder.kt
    │   │   └── PacketDecoder.kt
    │   └── body/
    │       ├── PacketBody.kt
    │       └── *Body.kt
    ├── network/
    │   ├── GameClient.kt
    │   ├── ConnectionPool.kt
    │   ├── ClientHandler.kt
    │   └── NettyCoroutineExt.kt
    ├── player/
    │   ├── VirtualPlayer.kt
    │   ├── PlayerState.kt
    │   └── PlayerType.kt
    ├── scenario/
    │   ├── dsl/
    │   ├── model/
    │   └── runner/
    ├── metrics/
    │   ├── MetricsCollector.kt
    │   └── TestReport.kt
    └── util/
        └── MockTokenGenerator.kt
```

## 실행 방법

```bash
# 빌드
./gradlew :game-load-test:build

# 실행
java -jar game-load-test.jar run --config config.yaml
```

## 시나리오 DSL 예시

```kotlin
val basicLoadTest = scenario("Basic Load Test") {
    config {
        playerCount = 60
        roomIdRange = -1L..-20L
        duration = 5.minutes
    }

    setup {
        authenticate()
        joinRoom()
        sendSceneChange()
        waitForGameStart()
    }

    game {
        continuousMove(duration = 10.seconds)
        ifMongging { digUpGgumtle(1) }
        ifMongdung { hitMongging(targetId = 1L) }
    }
}
```
