using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MVVM.Core
{
    /// <summary>
    /// 모든 ViewModel의 기본 클래스
    /// PropertyChanged 이벤트와 공통 기능 제공
    /// </summary>
    public abstract class BaseViewModel : MonoBehaviour, INotifyPropertyChanged
    {
        /// <summary>
        /// 프로퍼티 값이 변경될 때 발생하는 이벤트
        /// </summary>
        public event Action<string> PropertyChanged;

        /// <summary>
        /// 프로퍼티 값을 설정하고 변경 알림을 발생시킵니다
        /// </summary>
        /// <typeparam name="T">프로퍼티 타입</typeparam>
        /// <param name="field">백킹 필드 참조</param>
        /// <param name="value">새로운 값</param>
        /// <param name="propertyName">프로퍼티 이름 (자동으로 설정됨)</param>
        /// <returns>값이 변경되었으면 true, 그렇지 않으면 false</returns>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null
        )
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// PropertyChanged 이벤트를 발생시킵니다
        /// </summary>
        /// <param name="propertyName">변경된 프로퍼티 이름</param>
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(propertyName);

            if (EnableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] Property changed: {propertyName}");
            }
        }

        /// <summary>
        /// PropertyChanged 이벤트를 발생시킵니다 (public alias)
        /// </summary>
        /// <param name="propertyName">변경된 프로퍼티 이름</param>
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(propertyName);

            if (EnableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] Property changed: {propertyName}");
            }
        }

        /// <summary>
        /// 디버그 로그 활성화 여부
        /// </summary>
        [Header("Debug")]
        [SerializeField]
        protected bool EnableDebugLogs = false;

        /// <summary>
        /// ViewModel이 활성화되었을 때 호출
        /// </summary>
        protected virtual void OnEnable()
        {
            InitializeViewModel();
        }

        /// <summary>
        /// ViewModel이 비활성화되었을 때 호출
        /// </summary>
        protected virtual void OnDisable()
        {
            CleanupViewModel();
        }

        /// <summary>
        /// ViewModel 초기화 (서브클래스에서 오버라이드)
        /// </summary>
        protected virtual void InitializeViewModel()
        {
            // 서브클래스에서 구현
        }

        /// <summary>
        /// ViewModel 정리 (서브클래스에서 오버라이드)
        /// </summary>
        protected virtual void CleanupViewModel()
        {
            // 서브클래스에서 구현
        }

        /// <summary>
        /// 여러 프로퍼티가 변경되었음을 알림
        /// </summary>
        /// <param name="propertyNames">변경된 프로퍼티 이름들</param>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// 조건부 프로퍼티 변경 알림
        /// </summary>
        /// <param name="condition">알림 조건</param>
        /// <param name="propertyName">프로퍼티 이름</param>
        protected void OnPropertyChangedIf(
            bool condition,
            [CallerMemberName] string propertyName = null
        )
        {
            if (condition)
            {
                OnPropertyChanged(propertyName);
            }
        }
    }
}
