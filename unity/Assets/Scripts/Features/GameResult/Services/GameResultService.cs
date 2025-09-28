using Features.GameResult.Models;
using Features.GameResult.Messages;
using Features.GameResult.Services;
using MessagePipe;
using R3;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.GameResult.Services
{
    /// <summary>
    /// 게임 결과 처리 서비스 구현
    /// MainGameNetworkEventHandler에서 발행된 GameResultMessage를 구독하여 처리
    /// </summary>
    public class GameResultService : IGameResultService, IDisposable
    {
        private readonly ISubscriber<GameResultMessage> _gameResultSubscriber;
        private readonly CompositeDisposable _disposables = new();

        public GameResultData CurrentResult { get; private set; }

        public event Action<GameResultData> OnGameResultUpdated;
        public event Action OnShowGameResult;

        public GameResultService(ISubscriber<GameResultMessage> gameResultSubscriber)
        {
            _gameResultSubscriber = gameResultSubscriber;

            // GameResultMessage 구독
            _gameResultSubscriber
                .Subscribe(OnGameResultMessageReceived)
                .AddTo(_disposables);

            Debug.Log("[GameResultService] 서비스 초기화 완료");
        }

        /// <summary>
        /// GameResultMessage 수신 처리
        /// </summary>
        private void OnGameResultMessageReceived(GameResultMessage message)
        {
            Debug.Log($"[GameResultService] ===== GameResultMessage 수신 =====");
            Debug.Log($"[GameResultService] Result: {message.ResultData.Result}");
            Debug.Log($"[GameResultService] EscapedCount: {message.ResultData.EscapedMonggingCount}");
            Debug.Log($"[GameResultService] CoinReward: {message.ResultData.CoinReward}");
            Debug.Log($"[GameResultService] PlayerResults Count: {message.ResultData.PlayerResults?.Count ?? 0}");

            UpdateGameResult(message.ResultData);
            ShowGameResult();
        }

        public void UpdateGameResult(GameResultData resultData)
        {
            CurrentResult = resultData;
            OnGameResultUpdated?.Invoke(CurrentResult);

            Debug.Log($"[GameResultService] 게임 결과 업데이트: {CurrentResult.Result}, 탈출자 수: {CurrentResult.EscapedMonggingCount}");
        }

        public void ShowGameResult()
        {
            OnShowGameResult?.Invoke();
            Debug.Log("[GameResultService] 게임 결과 UI 표시 요청");
        }

        public void HideGameResult()
        {
            Debug.Log("[GameResultService] 게임 결과 UI 숨김 요청");
        }

        public void ReturnToLobby()
        {
            Debug.Log("[GameResultService] 로비로 돌아가기 - 비동기 씬 전환 시작");

            // MonoBehaviour가 필요하므로 GameObject를 찾아서 코루틴 실행
            var gameObject = new GameObject("SceneTransitionHelper");
            var helper = gameObject.AddComponent<SceneTransitionHelper>();
            helper.StartTransition();
        }

        /// <summary>
        /// 씬 전환을 위한 임시 헬퍼 클래스
        /// </summary>
        public class SceneTransitionHelper : MonoBehaviour
        {
            public void StartTransition()
            {
                StartCoroutine(LoadLobbySceneAsync());
            }

            private IEnumerator LoadLobbySceneAsync()
            {
                Debug.Log("[GameResultService] 백그라운드에서 로비 씬 로딩 시작");

                // 백그라운드에서 로비 씬 로딩 시작
                var asyncLoad = SceneManager.LoadSceneAsync("Lobby");
                asyncLoad.allowSceneActivation = false;

                // 로딩 진행상황 체크
                while (asyncLoad.progress < 0.9f)
                {
                    Debug.Log($"[GameResultService] 로비 씬 로딩 진행률: {asyncLoad.progress * 100}%");
                    yield return null;
                }

                Debug.Log("[GameResultService] 로비 씬 로딩 완료, 잠시 대기 후 전환");

                // 결과 화면을 잠시 더 보여주기 위해 1초 대기
                yield return new WaitForSeconds(1.0f);

                // 씬 활성화 (실제 전환)
                asyncLoad.allowSceneActivation = true;

                Debug.Log("[GameResultService] 로비 씬으로 전환 완료");

                // 헬퍼 GameObject 정리
                Destroy(gameObject);
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            Debug.Log("[GameResultService] 서비스 정리 완료");
        }
    }
}