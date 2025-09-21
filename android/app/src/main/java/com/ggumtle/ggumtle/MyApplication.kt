package com.ggumtle.ggumtle

import android.app.Application
import com.ggumtle.datastore.AuthManager
import com.ggumtle.domain.manager.GlobalInviteManager
import com.google.android.filament.utils.Utils
import dagger.hilt.android.HiltAndroidApp
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltAndroidApp
class MyApplication : Application() {

    companion object {
        private const val TAG = "MyApplication"

        lateinit var instance: MyApplication
            private set
    }

    private val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)

    @Inject
    lateinit var authManager: AuthManager

    @Inject
    lateinit var globalInviteManager: GlobalInviteManager

    override fun onCreate() {
        super.onCreate()
        instance = this

        // Filament 유틸리티 초기화
        Utils.init()

        applicationScope.launch {
            authManager.checkAutoLogin()
        }

        // 앱 시작 시 초대 알림 관찰 시작
        globalInviteManager.startObserving()
    }

    override fun onTerminate() {
        super.onTerminate()
        globalInviteManager.stopObserving()
    }
}