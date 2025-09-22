using System;
using System.Collections.Generic;
using Features.Chest.Models;
using R3;
using UnityEngine;

namespace Features.Chest.Services
{
    /// <summary>
    /// 상자 비즈니스 로직을 담당하는 서비스 인터페이스
    /// </summary>
    public interface IChestService
    {
        #region 상자 등록/관리

        /// <summary>
        /// 상자를 서비스에 등록
        /// </summary>
        void RegisterChest(string chestId, string chestName, Vector3 position, GameObject chestObject = null);

        /// <summary>
        /// 상자를 서비스에서 해제
        /// </summary>
        void UnregisterChest(string chestId);

        /// <summary>
        /// 특정 상자 데이터 조회
        /// </summary>
        ChestData GetChest(string chestId);

        /// <summary>
        /// 상자 존재 여부 확인
        /// </summary>
        bool HasChest(string chestId);

        /// <summary>
        /// 모든 상자 목록 조회
        /// </summary>
        List<ChestData> GetAllChests();

        #endregion

        #region 상자 상호작용

        /// <summary>
        /// 상자 열기
        /// </summary>
        bool OpenChest(string chestId);

        /// <summary>
        /// 현재 열린 상자 닫기
        /// </summary>
        void CloseCurrentChest();

        /// <summary>
        /// 현재 열린 상자에서 아이템 가져오기
        /// </summary>
        bool TakeItemFromCurrentChest(int slotIndex, int amount = -1);

        /// <summary>
        /// 현재 열린 상자의 슬롯 업데이트
        /// </summary>
        void UpdateCurrentChestSlot(int slotIndex, string itemId, int count);

        #endregion

        #region 서버 동기화

        /// <summary>
        /// 서버로부터 상자 데이터 동기화
        /// </summary>
        void SyncChestData(string chestId, ChestSlot[] serverSlots);

        /// <summary>
        /// 서버로부터 특정 슬롯 동기화
        /// </summary>
        void SyncChestSlot(string chestId, int slotIndex, string itemId, int count);

        #endregion

        #region 상태 관리

        /// <summary>
        /// 현재 열린 상자가 있는지 여부
        /// </summary>
        bool HasCurrentChest { get; }

        /// <summary>
        /// 현재 상자가 열려있는지 여부
        /// </summary>
        bool IsChestOpen { get; }

        /// <summary>
        /// 현재 상자 데이터
        /// </summary>
        ChestData CurrentChest { get; }

        /// <summary>
        /// 현재 상자 ID
        /// </summary>
        string CurrentChestId { get; }

        /// <summary>
        /// 현재 상자 상태 Observable
        /// </summary>
        Observable<ChestState> CurrentChestState { get; }

        /// <summary>
        /// UI 가시성 상태 Observable
        /// </summary>
        Observable<bool> IsUIVisible { get; }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        void Reset();

        /// <summary>
        /// 아이템이 있는 상자들만 조회
        /// </summary>
        List<ChestData> GetChestsWithItems();

        /// <summary>
        /// 전체 상자 개수
        /// </summary>
        int GetTotalChestCount();

        /// <summary>
        /// 모든 상자의 총 아이템 개수
        /// </summary>
        int GetTotalItemsInAllChests();

        #endregion
    }
}