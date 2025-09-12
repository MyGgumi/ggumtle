package com.ggumtle.data.websocket.di

import com.ggumtle.data.websocket.repository.WebSocketRepositoryImpl
import com.ggumtle.domain.websocket.repository.WebSocketRepository
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class WebsocketDataModule {

    @Binds
    @Singleton
    abstract fun bindWebSocketRepository(
        webSocketRepositoryImpl: WebSocketRepositoryImpl
    ): WebSocketRepository
}