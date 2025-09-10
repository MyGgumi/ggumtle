package com.example.data.websocket.di

import com.example.data.websocket.event.EventBusImpl
import com.example.domain.websocket.event.EventBus
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class EventBusModule {

    @Binds
    @Singleton
    abstract fun bindEventBus(
        eventBusImpl: EventBusImpl
    ): EventBus
}