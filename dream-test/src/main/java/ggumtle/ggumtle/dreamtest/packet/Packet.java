package ggumtle.ggumtle.dreamtest.packet;

public record Packet(
        PacketHeader header,
        byte[] data
) {
    public boolean hasData() {
        return data != null && data.length > 0;
    }

    public int getDataLength() {
        return data != null ? data.length : 0;
    }
}
