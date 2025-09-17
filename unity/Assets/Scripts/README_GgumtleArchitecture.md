# 🎯 꿈틀이 도메인 중심 아키텍처

Unity 프로젝트에서 꿈틀이 기능을 **도메인 중심 ViewModel + MessagePipe**로 재구성한 아키텍처 문서입니다.

## 📋 아키�ecture 개요

```
┌─────────────────────────────────────────────────────────────┐
│                     꿈틀이 도메인 구조                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [InteractionTriggerDetector]                              │
│         ↓ (MessagePipe)                                    │
│         ↓                                                  │
│  [GgumtleViewModel]                                        │
│      ├── State (Buried/Digging/Emerging/Feeding)          │
│      ├── HoldProgress                                      │
│      └── FoodAmount                                        │
│         ↓                                                  │
│    ┌────┴────┐                                            │
│    ↓         ↓                                            │
│ [GameObject] [UI]                                          │
│    View      View                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 🏗️ 구현된 컴포넌트

### 1. MessagePipe 메시지 시스템
**파일**: `Assets/Scripts/Messages/GgumtleMessages.cs`

```csharp
// 꿈틀이 감지/벗어남
GgumtleDetectedMessage
GgumtleLeftMessage

// 상태 변경 알림
GgumtleStateChangedMessage
GgumtlePurifiedMessage

// 진행률 업데이트
GgumtleHoldProgressMessage
GgumtleFoodAddedMessage

// 범용 알림
NotificationMessage
```

### 2. 순수 C# Service
**파일**: `Assets/Scripts/Services/IGgumtleService.cs`, `GgumtleServiceImpl.cs`

```csharp
public interface IGgumtleService
{
    // 꿈틀이 관리
    void RegisterGgumtle(string id, string name, Vector3 position);
    GgumtleData GetGgumtleData(string id);

    // 홀드 상호작용
    void StartHold(string id);
    void CompleteHold(string id);
    void CancelHold(string id);

    // 먹이주기
    bool FeedGgumtle(string id, int amount);
    void CompletePurification(string id);
}
```

**장점**:
- ✅ MonoBehaviour 의존성 제거
- ✅ 테스트 가능
- ✅ DI 친화적
- ✅ MessagePipe로 느슨한 결합

### 3. 도메인 중심 ViewModel
**파일**: `Assets/Scripts/ViewModels/GgumtleViewModel.cs`

```csharp
public class GgumtleViewModel
{
    // 관찰 가능한 상태들
    public readonly ReactiveProperty<GgumtleState> State;
    public readonly ReactiveProperty<float> HoldProgress;
    public readonly ReactiveProperty<int> CurrentFood;
    public readonly ReactiveProperty<bool> IsInRange;

    // 비즈니스 로직
    public async UniTask StartHold();
    public void CancelHold();
}
```

**특징**:
- 🎯 **도메인 중심**: 꿈틀이 전체 로직을 하나의 ViewModel에서 관리
- 🔗 **GameObject + UI 균형**: 두 View 모두 같은 ViewModel 구독
- 📡 **MessagePipe 통합**: 이벤트 기반 상태 동기화

### 4. 분리된 View 시스템

#### GameObject View
**파일**: `Assets/Scripts/Views/GgumtleGameObjectView.cs`

```csharp
public class GgumtleGameObjectView : MonoBehaviour
{
    [Inject] private GgumtleViewModel _viewModel;

    void Start()
    {
        // 상태 변경에 따른 애니메이션/이펙트
        _viewModel.State.Subscribe(OnStateChanged).AddTo(this);
        _viewModel.HoldProgress.Subscribe(OnDiggingProgress).AddTo(this);
    }
}
```

#### UI View
**파일**: `Assets/Scripts/Views/GgumtleUIView.cs`

```csharp
public class GgumtleUIView : MonoBehaviour
{
    [Inject] private GgumtleViewModel _viewModel;

    void Start()
    {
        // UI 상태 동기화
        _viewModel.IsInRange.Subscribe(ShowPanel).AddTo(this);
        _viewModel.InteractionText.Subscribe(UpdateText).AddTo(this);
        _viewModel.HoldProgress.Subscribe(UpdateProgressBar).AddTo(this);
    }
}
```

### 5. VContainer DI 설정
**파일**: `Assets/Scripts/DI/GameLifetimeScope.cs`

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // MessagePipe 등록
        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker<GgumtleDetectedMessage>(options);

        // Service 등록
        builder.Register<IGgumtleService, GgumtleServiceImpl>(Lifetime.Singleton);

        // ViewModel 등록
        builder.Register<GgumtleViewModel>(Lifetime.Singleton);
    }
}
```

### 6. 마이그레이션된 Detector
**파일**: `Assets/Scripts/Interaction/InteractionTriggerDetector.cs`

```csharp
void OnTriggerEnter(Collider other)
{
    var ggumtle = other.GetComponent<InteractableGgumtle>();
    if (ggumtle != null)
    {
        // MessagePipe로 이벤트 발행
        _ggumtleDetectedPublisher.Publish(new GgumtleDetectedMessage(...));
        return;
    }

    // 다른 상호작용 객체는 기존 방식 유지
    var interactable = other.GetComponent<IInteractable>();
    OnInteractableEntered?.Invoke(interactable);
}
```

## 🔄 데이터 흐름

### 1. 꿈틀이 감지
```
Player enters Trigger → InteractionTriggerDetector
→ GgumtleDetectedMessage → GgumtleViewModel
→ State/UI 업데이트 → Views 반응
```

### 2. 홀드 상호작용
```
UI Button Click → GgumtleViewModel.StartHold()
→ GgumtleService.StartHold() → State Change Message
→ GameObject/UI Views 업데이트
```

### 3. 상태 동기화
```
Service State Change → MessagePipe → ViewModel
→ ReactiveProperty → Multiple Views 동시 업데이트
```

## 🎯 아키텍처 장점

### 1. 도메인 중심 설계
- **명확한 책임**: 꿈틀이 로직이 ViewModel에 집중
- **단일 진실 원천**: 하나의 ViewModel이 모든 상태 관리
- **쉬운 확장**: 새로운 View 추가시 ViewModel 구독만 하면 됨

### 2. 느슨한 결합
- **MessagePipe**: 컴포넌트 간 직접 참조 제거
- **DI 컨테이너**: 의존성 자동 주입
- **Interface 분리**: 테스트 가능한 구조

### 3. 성능 최적화
- **구조체 메시지**: GC 압박 최소화
- **조건부 발행**: 필요한 경우만 이벤트 발행
- **Reactive 스트림**: 효율적인 상태 동기화

### 4. 호환성 유지
- **점진적 마이그레이션**: 기존 코드와 공존
- **기존 이벤트 유지**: 다른 상호작용 객체 호환
- **레거시 지원**: MonoBehaviour Service 병행

## 🧪 테스트 및 디버깅

### Debug Helper
**파일**: `Assets/Scripts/Debug/GgumtleTestHelper.cs`

- 실시간 ViewModel 상태 표시
- 에디터에서 홀드 테스트
- 컨텍스트 메뉴로 상태 로깅

### 사용법
1. 씬에 `GameLifetimeScope` 추가
2. Player에 `InteractionTriggerDetector` 추가 (VContainer 주입 필요)
3. 꿈틀이 GameObject에 `InteractableGgumtle` + `GgumtleGameObjectView` 추가
4. UI에 `GgumtleUIView` 추가
5. 디버깅용으로 `GgumtleTestHelper` 추가

## 🚀 향후 확장 계획

1. **인벤토리 시스템 연동**: 빛젤리 소모 로직 구현
2. **서버 동기화**: 꿈틀이 상태 서버 저장/로드
3. **애니메이션 시스템**: Timeline 기반 상태 전환
4. **사운드 시스템**: 상태별 효과음 재생
5. **다른 도메인 적용**: 상자, 인벤토리도 같은 패턴으로 리팩토링

## 💡 핵심 원칙

1. **도메인 우선**: 기능 중심으로 ViewModel 구성
2. **느슨한 결합**: MessagePipe로 컴포넌트 독립성 확보
3. **테스트 가능**: 순수 C# Service로 비즈니스 로직 분리
4. **점진적 개선**: 기존 시스템과 공존하며 안전하게 마이그레이션

---

*이 아키텍처는 Unity의 현실적 제약을 고려하면서도 클린 아키텍처의 장점을 최대한 활용하는 실용적 접근입니다.*