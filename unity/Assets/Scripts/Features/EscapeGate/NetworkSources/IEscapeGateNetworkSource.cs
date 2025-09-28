using System;
using Cysharp.Threading.Tasks;
using Networks.Game;

namespace Features.EscapeGate.NetworkSources
{
    /// <summary>
    /// 탈출 게이트 관련 네트워크 통신 인터페이스
    /// </summary>
    public interface IEscapeGateNetworkSource
    {
        /// <summary>
        /// 탈출 시도 요청
        /// </summary>
        /// <param name="gateId">탈출구 ID</param>
        /// <returns>탈출 시도 응답</returns>
        UniTask<ExitAttemptCommand> AttemptEscapeAsync(int gateId);
    }
}