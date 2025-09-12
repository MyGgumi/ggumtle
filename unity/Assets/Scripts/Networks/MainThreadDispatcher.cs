using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace Networks
{
    /// <summary>
    /// 네트워크 스레드에서 메인 스레드로 작업을 전달하는 디스패처
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private readonly ConcurrentQueue<Func<Task>> _taskQueue = new ConcurrentQueue<Func<Task>>();
        private readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 MainThreadDispatcher를 찾거나 생성
                    _instance = FindObjectOfType<MainThreadDispatcher>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MainThreadDispatcher");
                        _instance = go.AddComponent<MainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Action 큐 처리
            while (_actionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MainThreadDispatcher Action 실행 중 오류: {ex.Message}");
                }
            }

            // Task 큐 처리
            while (_taskQueue.TryDequeue(out var taskFunc))
            {
                try
                {
                    _ = taskFunc?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MainThreadDispatcher Task 실행 중 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 메인 스레드에서 Action 실행
        /// </summary>
        /// <param name="action">실행할 Action</param>
        public void Enqueue(Action action)
        {
            if (action == null) return;
            _actionQueue.Enqueue(action);
        }

        /// <summary>
        /// 메인 스레드에서 Task 실행
        /// </summary>
        /// <param name="taskFunc">실행할 Task 함수</param>
        public void EnqueueTask(Func<Task> taskFunc)
        {
            if (taskFunc == null) return;
            _taskQueue.Enqueue(taskFunc);
        }

        /// <summary>
        /// 메인 스레드에서 Task 실행 (async/await 지원)
        /// </summary>
        /// <param name="taskFunc">실행할 Task 함수</param>
        /// <returns>Task</returns>
        public async Task EnqueueAsync(Func<Task> taskFunc)
        {
            if (taskFunc == null) return;

            var tcs = new TaskCompletionSource<bool>();
            
            _taskQueue.Enqueue(async () =>
            {
                try
                {
                    await taskFunc();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            await tcs.Task;
        }

        /// <summary>
        /// 메인 스레드에서 Action 실행 (async/await 지원)
        /// </summary>
        /// <param name="action">실행할 Action</param>
        /// <returns>Task</returns>
        public async Task EnqueueAsync(Action action)
        {
            if (action == null) return;

            var tcs = new TaskCompletionSource<bool>();
            
            _actionQueue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            await tcs.Task;
        }
    }
}
