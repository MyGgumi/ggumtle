package com.example.data.websocket.di

import com.example.data.websocket.repository.WebSocketRepositoryImpl
import com.example.domain.websocket.repository.WebSocketRepository
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent

@Module
@InstallIn(SingletonComponent::class)
abstract class RepositoryModule {

    @Binds
    abstract fun bindWebSocketRepository(
        webSocketRepositoryImpl: WebSocketRepositoryImpl
    ): WebSocketRepository
}