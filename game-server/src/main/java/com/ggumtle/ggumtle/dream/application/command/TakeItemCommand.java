package com.ggumtle.ggumtle.dream.application.command;

public record TakeItemCommand(
    int boxId,
    int index
) {
}
