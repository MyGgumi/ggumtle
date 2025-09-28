using Cysharp.Threading.Tasks;
using Features.FieldItem.Models;

namespace Features.FieldItem.Services
{
    /// <summary>
    /// 필드 아이템 서비스 인터페이스
    /// </summary>
    public interface IFieldItemService
    {
        /// <summary>
        /// 필드 아이템 사용 요청 (void - 브로드캐스트로 결과 수신)
        /// </summary>
        /// <param name="fieldItemId">사용할 필드 아이템 ID</param>
        void UseFieldItem(int fieldItemId);

        /// <summary>
        /// 서버 스폰 정보로부터 필드 아이템 등록
        /// </summary>
        /// <param name="fieldItemId">필드 아이템 ID</param>
        /// <param name="itemType">필드 아이템 타입</param>
        void RegisterFieldItem(int fieldItemId, FieldItemType itemType);

        /// <summary>
        /// 필드 아이템 ID로 타입 조회
        /// </summary>
        /// <param name="fieldItemId">필드 아이템 ID</param>
        /// <returns>필드 아이템 타입 (null이면 존재하지 않음)</returns>
        FieldItemType? GetFieldItemType(int fieldItemId);

        /// <summary>
        /// 필드 아이템 사용 가능 여부 확인
        /// </summary>
        /// <param name="fieldItemId">필드 아이템 ID</param>
        /// <returns>사용 가능 여부</returns>
        bool CanUseFieldItem(int fieldItemId);
    }
}