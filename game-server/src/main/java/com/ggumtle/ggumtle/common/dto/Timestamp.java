package com.ggumtle.ggumtle.common.dto;

public final class Timestamp {
    public final long value;

    public Timestamp(long value) {
        this.value = value;
    }

    @Override
    public String toString() {
        return "Timestamp[" + value + "]";
    }
}
