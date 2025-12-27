package com.ggumtle.loadtest

import com.ggumtle.loadtest.cli.LoadTestCommand
import com.ggumtle.loadtest.cli.RunCommand
import com.github.ajalt.clikt.core.subcommands

fun main(args: Array<String>) {
    LoadTestCommand()
        .subcommands(RunCommand())
        .main(args)
}
