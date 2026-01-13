package com.ggumtle.loadtest.network

import io.netty.channel.Channel
import io.netty.channel.ChannelFuture
import io.netty.util.concurrent.Future
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * Awaits a ChannelFuture and returns the Channel when connected
 */
suspend fun ChannelFuture.awaitChannel(): Channel = suspendCancellableCoroutine { cont ->
    addListener { future ->
        if (future.isSuccess) {
            cont.resume(channel())
        } else {
            cont.resumeWithException(future.cause() ?: Exception("Connection failed"))
        }
    }
}

/**
 * Awaits a ChannelFuture completion
 */
suspend fun ChannelFuture.awaitComplete(): Unit = suspendCancellableCoroutine { cont ->
    addListener { future ->
        if (future.isSuccess) {
            cont.resume(Unit)
        } else {
            cont.resumeWithException(future.cause() ?: Exception("Operation failed"))
        }
    }
}

/**
 * Awaits a generic Netty Future
 */
suspend fun <V> Future<V>.await(): V = suspendCancellableCoroutine { cont ->
    addListener { future ->
        if (future.isSuccess) {
            @Suppress("UNCHECKED_CAST")
            cont.resume(future.now as V)
        } else {
            cont.resumeWithException(future.cause() ?: Exception("Future failed"))
        }
    }
}
