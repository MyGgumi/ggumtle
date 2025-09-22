package com.ggumtle.data.rest.di

import com.ggumtle.data.rest.remote.service.MemberService
import com.ggumtle.data.rest.remote.service.AuthService
import com.ggumtle.data.rest.remote.service.GrowthService
import com.ggumtle.data.rest.remote.service.MissionService
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton
import kotlin.jvm.java

@Module
@InstallIn(SingletonComponent::class)
internal object ServiceModule {

    @Provides
    @Singleton
    fun provideAuthService(retrofit: Retrofit): AuthService {
        return retrofit.create(AuthService::class.java)
    }

    @Provides
    @Singleton
    fun provideUserService(retrofit: Retrofit): MemberService {
        return retrofit.create(MemberService::class.java)
    }

    @Provides
    @Singleton
    fun provideGrowthService(retrofit: Retrofit): GrowthService {
        return retrofit.create(GrowthService::class.java)
    }

    @Provides
    @Singleton
    fun provideMissionService(retrofit: Retrofit): MissionService {
        return retrofit.create(MissionService::class.java)
    }

}