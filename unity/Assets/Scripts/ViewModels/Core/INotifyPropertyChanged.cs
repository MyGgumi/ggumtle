using System;

namespace MVVM.Core
{
    /// <summary>
    /// 프로퍼티 변경 알림을 지원하는 인터페이스
    /// </summary>
    public interface INotifyPropertyChanged
    {
        /// <summary>
        /// 프로퍼티 값이 변경될 때 발생하는 이벤트
        /// </summary>
        event Action<string> PropertyChanged;
    }
}