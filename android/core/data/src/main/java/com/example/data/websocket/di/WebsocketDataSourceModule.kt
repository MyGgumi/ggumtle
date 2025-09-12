package com.ggumtle.data.websocket.di

import com.ggumtle.data.websocket.remote.datasource.WebSocketRemoteDataSource
import com.ggumtle.data.websocket.remote.datasource.WebSocketRemoteDataSourceImpl
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class WebsocketDataSourceModule {

    @Binds
    @Singleton
    abstract fun bindWebSocketRemoteDataSource(
        webSocketRemoteDataSourceImpl: WebSocketRemoteDataSourceImpl
    ): WebSocketRemoteDataSource
}