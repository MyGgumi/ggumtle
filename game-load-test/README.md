# Game Load Test

Netty 기반 게임 서버(game-server)의 부하 테스트를 위한 클라이언트 도구

## 기술 스택

- **Language**: Kotlin 2.0.21
- **Async**: Kotlin Coroutines 1.9.0
- **Network**: Netty Client 4.2.x
- **Auth**: JJWT 0.12.5 (game-server와 동일)
- **CLI**: Clikt 4.2.2
- **Config**: kaml (YAML parser)
- **Logging**: kotlin-logging + Logback

## 요구 사항

| 항목 | 요구 사항 |
|------|----------|
| **Java** | 21 (필수, Java 25는 Gradle 호환성 문제) |
| **game-server** | 실행 중이어야 함 |

### 환경변수

`.env` 파일 또는 시스템 환경변수로 설정 (우선순위: 시스템 환경변수 > .env 파일 > 기본값)

| 변수 | 필수 | 기본값 | 설명 |
|------|:----:|--------|------|
| `ACCESS_SECRET` | O | - | JWT 시크릿 (game-server와 동일, BASE64) |
| `GAME_SERVER_HOST` | X | localhost | 게임 서버 호스트 |
| `GAME_SERVER_PORT` | X | 9000 | 게임 서버 포트 |

## 프로젝트 구조

```
game-load-test/
├── build.gradle.kts          # 빌드 설정
├── settings.gradle.kts
├── README.md
├── CLAUDE.md                 # AI 어시스턴트용 컨텍스트
├── .env                      # 환경변수 (선택, gitignore)
└── src/main/kotlin/com/ggumtle/loadtest/
    ├── Application.kt        # 진입점
    │
    ├── cli/                  # CLI 인터페이스
    │   ├── LoadTestCommand.kt
    │   └── RunCommand.kt
    │
    ├── config/               # 설정 관리
    │   ├── EnvLoader.kt      # .env 파일 자동 로딩
    │   ├── ConfigLoader.kt
    │   └── LoadTestConfig.kt
    │
    ├── protocol/             # 패킷 프로토콜
    │   ├── PacketType.kt     # 패킷 타입 정의
    │   ├── Packet.kt
    │   ├── PacketHeader.kt   # 14-byte 헤더
    │   ├── codec/
    │   │   ├── PacketEncoder.kt
    │   │   └── PacketDecoder.kt
    │   └── body/
    │       └── PacketBody.kt # 패킷 본문 클래스들
    │
    ├── network/              # 네트워크 레이어
    │   ├── GameClient.kt     # TCP 클라이언트
    │   ├── ConnectionPool.kt
    │   ├── ClientHandler.kt
    │   ├── ClientInitializer.kt
    │   └── NettyCoroutineExt.kt  # Coroutine 확장
    │
    ├── player/               # 가상 플레이어
    │   ├── VirtualPlayer.kt  # 플레이어 시뮬레이션
    │   ├── PlayerState.kt
    │   └── PlayerType.kt
    │
    ├── scenario/             # 시나리오 DSL
    │   ├── dsl/
    │   │   └── ScenarioDsl.kt
    │   ├── model/
    │   │   ├── Scenario.kt
    │   │   └── PlayerGroup.kt  # 그룹 기반 플레이어 관리
    │   └── runner/
    │       └── ScenarioRunner.kt
    │
    ├── metrics/              # 메트릭 수집
    │   └── MetricsCollector.kt  # TestReport 포함
    │
    └── util/
        └── MockTokenGenerator.kt  # JWT 토큰 생성
```

## 빌드

```bash
# Java 21 설정
export JAVA_HOME=/path/to/java21

# 빌드
./gradlew build

# JAR 위치: build/libs/game-load-test-1.0.0.jar
```

## 사용법

### 1. 환경 설정

```bash
# game-server의 .env 파일에서 ACCESS_SECRET 값을 확인
# game-load-test에도 동일한 값 설정
export ACCESS_SECRET="your-base64-encoded-secret"
```

### 2. 실행

```bash
# 기본 실행 (환경변수 또는 기본값 사용)
java -jar build/libs/game-load-test-1.0.0.jar run

# 옵션 지정
java -jar build/libs/game-load-test-1.0.0.jar run \
  --host localhost \
  --port 9000 \
  --players 30 \
  --duration 120
```

### 3. CLI 옵션

| 옵션 | 단축 | 기본값 | 설명 |
|------|------|--------|------|
| `--config` | `-c` | config.yaml | 설정 파일 경로 |
| `--host` | `-h` | 환경변수 | 게임 서버 호스트 |
| `--port` | `-p` | 환경변수 | 게임 서버 포트 |
| `--players` | `-n` | 5 | 가상 플레이어 수 |
| `--duration` | `-d` | 11 | 테스트 시간 (초) |

> **참고**: `rampUpDuration`(10초), `playersPerRoom`(5) 등은 현재 하드코딩됨

### 4. 설정 파일 (config.yaml)

```yaml
server:
  host: localhost
  port: 9000

test:
  playerCount: 60
  roomIdStart: -1
  roomIdEnd: -20
  playersPerRoom: 3
  durationSeconds: 300
  rampUpSeconds: 30
  moveIntervalMs: 16

auth:
  useMockToken: true
```

## 테스트 시나리오

### 기본 흐름

```
1. TCP 연결 (Netty, ramp-up으로 점진적 연결)
       ↓
2. 인증 (VERIFY_TOKEN 패킷, JWT 토큰)
       ↓
3. 방 입장 (ROOM_JOIN 패킷, roomId: -1 ~ -20)
       ↓
4. 씬 로드 (SCENE_CHANGE 패킷)
       ↓
5. 게임 시작 대기 (GAME_START 패킷 수신)
       ↓
6. 게임 플레이 (PLAYER_MOVE 등 지속적 전송)
       ↓
7. 연결 종료
       ↓
8. 결과 리포트 출력
```

### 시나리오 DSL 예시

#### 기본 setup 패턴

```kotlin
val loadTest = scenario("Custom Load Test") {
    config {
        playerCount = 100
        roomIdStart = -1L
        roomIdEnd = -20L
        playersPerRoom = 3
        duration = 5.minutes
        rampUpDuration = 30.seconds
    }

    setup {
        authenticate()
        joinRoom()
        sendSceneChange()
        waitForGameStart()
    }

    game {
        continuousMove(duration = 4.minutes)

        ifMongging {
            digUpGgumtle(ggumtleId = 1)
            feedGgumtle(ggumtleId = 1, duration = 3.seconds)
        }

        ifMongdung {
            hitMongging(targetId = 1L)
            mongdungSkill(skillType = 1)
        }
    }

    teardown {
        disconnect()
    }
}
```

#### groupSetup 패턴 (리더/멤버 동기화)

```kotlin
val groupLoadTest = scenario("Group Synchronized Test") {
    config {
        playerCount = 60
        roomIdStart = -1L
        roomIdEnd = -20L
        playersPerRoom = 3
        duration = 5.minutes
    }

    // 그룹 기반 셋업 - 리더가 방 생성, 멤버가 조인 대기
    groupSetup { group ->
        authenticate()
        group.coordinatedRoomSetup()  // 리더/멤버 자동 동기화
        sendSceneChange()
        waitForGameStart()
    }

    game {
        repeat(10) {
            continuousMove(duration = 30.seconds)
            wait(1.seconds)
        }
    }

    teardown {
        disconnect()
    }
}
```

### 사용 가능한 액션

**Setup Phase** (setup { } 또는 groupSetup { })
- `authenticate()` - JWT 토큰 인증
- `joinRoom()` - 방 입장 (setup에서만)
- `sendSceneChange()` - 씬 로드 신호
- `waitForGameStart()` - 게임 시작 대기
- `wait(duration)` - 대기

**GroupSetup 전용** (groupSetup { group -> })
- `group.isLeader(player)` - 리더 여부 확인
- `group.coordinatedRoomSetup()` - 리더/멤버 동기화 방 설정

**Game Phase**
- `move(x, y, z, vx, vy, vz)` - 이동
- `randomMove(count, intervalMs)` - 랜덤 이동
- `continuousMove(duration, intervalMs)` - 지속 이동
- `repeat(times) { }` - 반복 실행
- `wait(duration)` - 대기

*Ggumtle 상호작용:*
- `digUpGgumtle(ggumtleId)` - 꿈틀이 파기
- `stopDigging()` - 파기 중지
- `startFeed(ggumtleId)` - 먹이기 시작
- `stopFeed()` - 먹이기 중지
- `feedGgumtle(ggumtleId, duration)` - 먹이기 (시작+대기+중지)

*박스/아이템:*
- `openBox(boxId)` / `closeBox(boxId)` - 박스 열기/닫기
- `takeItem(boxId, itemIndex)` - 아이템 획득
- `putItem(boxId, itemId)` - 아이템 넣기

*전투:*
- `hitMongging(targetId, vx, vy, vz)` - 몽이 공격
- `startRevive(targetId)` / `stopRevive()` - 부활
- `mongdungSkill(skillType)` - 몽둥이 스킬
- `attackWithItem(x, y, z, itemId)` - 아이템으로 공격
- `useFieldItem(itemId)` - 필드 아이템 사용
- `useDefibrillator()` - 제세동기 사용

*탈출:*
- `escape(exitId)` - 탈출

*조건부 실행:*
- `ifMongging { }` - 꿈틀 플레이어 전용
- `ifMongdung { }` - 몽둥이 플레이어 전용

**Teardown Phase**
- `disconnect()` - 연결 종료
- `wait(duration)` - 대기

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
| VERIFY_TOKEN | 1 | UTF-8 string (JWT) |
| ROOM_JOIN | 10 | Long (roomId) |
| SCENE_CHANGE | 30 | 없음 |
| PLAYER_MOVE | 40 | 6 ints (x, y, z, vx, vy, vz) |
| HIT_MONGGING | 60 | 3 ints + Long |
| DIG_UP_GGUMTLE | 100 | Int (ggumtleId) |
| ESCAPE | 140 | Int (exitId) |

### 주요 패킷 (Server → Client)

| Type | ID | Description |
|------|-----|-------------|
| VERIFY_TOKEN | 2 | 인증 결과 |
| ROOM_JOIN | 11 | 방 입장 결과 |
| GAME_START | 35 | 게임 시작 신호 |
| END | 200 | 게임 종료 |

## 결과 리포트

테스트 완료 후 다음 메트릭이 출력됩니다:

```
=== Load Test Report ===
Duration: 300s
Players: 60
Total Moves: 180,000

Latency (ms):
  auth:     avg=12.3  p50=10.0  p90=18.5  p99=45.2
  joinRoom: avg=8.5   p50=7.2   p90=12.1  p99=28.3

Counters:
  moves: 180,000
  digUps: 1,200
  hits: 800

Errors: 0
```

## 주의사항

1. **Java 버전**
   - Java 21 필수
   - Java 25는 Gradle 호환성 문제로 빌드 불가

2. **환경변수**
   - `ACCESS_SECRET`이 game-server와 동일해야 함
   - 불일치 시 모든 인증 실패

3. **game-server 상태**
   - 테스트 전 game-server가 실행 중이어야 함
   - 테스트 방 ID: -1 ~ -20 (RoomCreator에서 자동 생성)
   - 방당 최대 6명

4. **리소스**
   - 다수의 동시 연결은 시스템 리소스 소모
   - 점진적 ramp-up으로 서버 부하 완화
   - 파일 디스크립터 제한 확인 필요 (`ulimit -n`)

5. **네트워크**
   - TCP 기반 바이너리 프로토콜 (WebSocket 아님)
   - 패킷 헤더 14바이트 + 가변 길이 바디

## 트러블슈팅

### 인증 실패
```
원인: ACCESS_SECRET 불일치
해결: game-server의 .env에서 ACCESS_SECRET 값 확인 후 동일하게 설정
```

### 연결 거부
```
원인: game-server 미실행 또는 포트 불일치
해결: game-server 실행 상태 및 포트 확인
```

### 방 입장 실패
```
원인: 테스트 방(-1 ~ -20)이 생성되지 않음
해결: game-server의 RoomCreator 설정 확인
```

### 빌드 실패
```
원인: Java 25 사용
해결: Java 21로 변경 (export JAVA_HOME=/path/to/java21)
```

## 참조

- `game-server/src/main/java/.../packet/` - 패킷 정의
- `game-server/src/main/java/.../auth/JwtService.java` - JWT 검증 로직
- `game-server/src/main/java/.../init/RoomCreator.java` - 테스트 방 생성