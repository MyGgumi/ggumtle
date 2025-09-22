package com.ggumtle.domain.websocket.usecase.social

import com.ggumtle.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class SearchMembersUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke(keyword: String, page: Int = 0, size: Int = 20) {
        require(keyword.isNotBlank()) { "검색 키워드는 비어있을 수 없습니다." }
        require(page >= 0) { "페이지 번호는 0 이상이어야 합니다." }
        require(size > 0) { "페이지 크기는 0보다 커야 합니다." }

        webSocketRepository.searchMembers(keyword, page, size)
    }
}