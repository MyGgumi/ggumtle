package com.ggumtle.loadtest.cli

import com.github.ajalt.clikt.core.CliktCommand
import com.github.ajalt.clikt.core.subcommands

/**
 * Main CLI command
 */
class LoadTestCommand : CliktCommand(
    name = "game-load-test",
    help = "Load testing tool for Ggumtle game server"
) {
    override fun run() = Unit
}
