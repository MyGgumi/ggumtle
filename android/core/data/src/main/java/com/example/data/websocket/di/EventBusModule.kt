package com.ggumtle.data.websocket.di

import com.ggumtle.data.websocket.event.EventBusImpl
import com.ggumtle.domain.websocket.event.EventBus
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