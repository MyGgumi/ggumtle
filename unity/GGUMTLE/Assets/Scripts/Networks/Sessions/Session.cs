using Networks.Attributes;
using Networks.Packets;
using UnityEngine;

namespace Networks.Sessions
{
    public class Session
    {
        private long _sessionId;
        
        /// <summary>
        /// 세션 ID를 설정합니다.
        /// </summary>
        /// <param name="sessionId">설정할 세션 ID</param>
        public void SetSessionId(long sessionId)
        {
            _sessionId = sessionId;
            Debug.Log($"세션 ID 설정: {sessionId}");
        }
        
        /// <summary>
        /// 현재 세션 ID를 반환합니다.
        /// </summary>
        /// <returns>세션 ID</returns>
        public long GetSessionId()
        {
            return _sessionId;
        }
        
        [CommandHandler(PacketType.VerifyTokenResponse)]
        public void HandleVerifyTokenResponse(VerifyTokenCommand response)
        {
            Debug.Log($"{response.SessionId} 응답 들어옴");
            
            _sessionId = response.SessionId;
        }
    }
}