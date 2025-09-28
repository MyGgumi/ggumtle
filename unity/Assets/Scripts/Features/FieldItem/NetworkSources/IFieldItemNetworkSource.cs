namespace Features.FieldItem.NetworkSources
{
    /// <summary>
    /// 필드 아이템 사용 관련 네트워크 통신 인터페이스
    /// </summary>
    public interface IFieldItemNetworkSource
    {
        /// <summary>
        /// 필드 아이템 사용 요청 (void - 응답 없음, 브로드캐스트로 받음)
        /// </summary>
        /// <param name="fieldItemId">사용할 필드 아이템 ID</param>
        void UseFieldItem(int fieldItemId);
    }
}