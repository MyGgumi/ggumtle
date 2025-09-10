package com.example.data.websocket.di

import com.example.data.websocket.repository.WebSocketRepositoryImpl
import com.example.domain.websocket.repository.WebSocketRepository
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