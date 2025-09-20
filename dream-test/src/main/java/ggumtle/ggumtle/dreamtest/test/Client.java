package ggumtle.ggumtle.dreamtest.test;

import io.netty.bootstrap.Bootstrap;
import io.netty.channel.Channel;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.nio.NioSocketChannel;
import jakarta.annotation.PreDestroy;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketDecoder;
import ggumtle.ggumtle.dreamtest.config.PacketEncoder;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import ggumtle.ggumtle.dreamtest.packet.Packet;
import ggumtle.ggumtle.dreamtest.packet.PacketHeader;
import ggumtle.ggumtle.dreamtest.packet.ReceivePacketType;

import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

@Slf4j
public class Client {
    private EventLoopGroup workerGroup;
    private Channel channel;

    protected Client(String host, int port, PacketHandler packetHandler) {
        this.workerGroup = new NioEventLoopGroup();

        try {
            Bootstrap bootstrap = new Bootstrap();
            bootstrap.group(workerGroup)
                    .channel(NioSocketChannel.class)
                    .option(ChannelOption.SO_KEEPALIVE, true)
                    .handler(new ChannelInitializer<Channel>() {
                        @Override
                        protected void initChannel(Channel ch) {
                            ch.pipeline()
                                    .addLast(new PacketDecoder())
                                    .addLast(new PacketEncoder())
                                    .addLast(packetHandler);
                        }
                    });

            ChannelFuture future = bootstrap.connect(new InetSocketAddress(host, port)).sync();
            channel = future.channel();

            log.info("서버에 성공적으로 연결되었습니다.");
        } catch (Exception e) {
            log.error("서버 연결 중 오류 발생", e);
            e.printStackTrace();
        }
    }

    @PreDestroy
    public void stop() {
        log.info("클라이언트 연결 종료 중...");

        if (channel != null) {
            channel.close();
        }

        if (workerGroup != null) {
            workerGroup.shutdownGracefully();
        }

        log.info("클라이언트 연결이 종료되었습니다.");
    }

    public void send(Packet packet) {
        if (channel != null && channel.isActive()) {
            log.info("메시지 전송: {}", packet);
            channel.writeAndFlush(packet);
        } else {
            log.warn("채널이 열려있지 않아 메시지를 전송할 수 없습니다.");
        }
    }

    public void sendAuthMessage(String accessToken) {
        byte[] data = accessToken.getBytes(StandardCharsets.UTF_8);
        ReceivePacketType receivePacketType = ReceivePacketType.VERIFY_TOKEN;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendJoinRoomMessage(long roomId) {
        byte[] command = ByteBuffer.allocate(Long.BYTES).putLong(roomId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.ROOM_JOIN;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, command.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, command);

        send(packet);
    }

    public void sendSceneChangeMessage() {
        ReceivePacketType receivePacketType = ReceivePacketType.SCENE_CHANGE;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, 0, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, null);

        send(packet);
    }

    public void sendMoveMessage(int x, int y, int z) {
        byte[] data = ByteBuffer.allocate(12).putInt(x).putInt(y).putInt(z).array();
        ReceivePacketType receivePacketType = ReceivePacketType.PLAYER_MOVE;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendHitMonggingMessage() {
        byte[] data = ByteBuffer.allocate(20).putInt(1).putInt(1).putInt(1).putLong(1L).array();
        ReceivePacketType receivePacketType = ReceivePacketType.HIT_MONGGING;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendStartReviveMessage(long targetMonggingId) {
        byte[] data = ByteBuffer.allocate(8).putLong(targetMonggingId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.START_REVIVE;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendStopReviveMessage(long targetMonggingId) {
        ReceivePacketType receivePacketType = ReceivePacketType.STOP_REVIVE;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, 0, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, null);

        send(packet);
    }

    public void sendMongdungSkillMessage(int skillType) {
        byte[] data = ByteBuffer.allocate(4).putInt(skillType).array();
        ReceivePacketType receivePacketType = ReceivePacketType.MONGDUNG_SKILL;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void attackWithItem(int itemId, int x, int y, int z) {
        byte[] data = ByteBuffer.allocate(4 * 3 + 4).putInt(x).putInt(y).putInt(z).putInt(itemId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.ATTACK_WITH_ITEM;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void useFieldItem(int itemId) {
        byte[] data = ByteBuffer.allocate(4).putInt(itemId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.USE_FIELD_ITEM;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendShowBoxMessage(int boxId) {
        byte[] data = ByteBuffer.allocate(4).putInt(boxId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.SHOW_BOX;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendTakeItemMessage(int boxId, int index) {
        byte[] data = ByteBuffer.allocate(8).putInt(boxId).putInt(index).array();
        ReceivePacketType receivePacketType = ReceivePacketType.TAKE_ITEM_FROM_BOX;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendPutItemMessage(int boxId, int index) {
        byte[] data = ByteBuffer.allocate(8).putInt(boxId).putInt(index).array();
        ReceivePacketType receivePacketType = ReceivePacketType.PUT_ITEM_TO_BOX;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendCloseBoxMessage(int boxId) {
        byte[] data = ByteBuffer.allocate(4).putInt(boxId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.CLOSE_BOX;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendDigUpMessage(int ggumtleId) {
        byte[] data = ByteBuffer.allocate(4).putInt(ggumtleId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.DIG_UP_GGUMTLE;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendStopDiggingMessage() {
        ReceivePacketType receivePacketType = ReceivePacketType.STOP_DIGGING;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, 0, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, null);

        send(packet);
    }

    public void sendStartFeedMessage(int ggumtleId) {
        byte[] data = ByteBuffer.allocate(4).putInt(ggumtleId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.START_FEED;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }

    public void sendStopFeedMessage() {
        ReceivePacketType receivePacketType = ReceivePacketType.STOP_FEED;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, 0, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, null);

        send(packet);
    }

    public void sendEscapeMessage(int exitId) {
        byte[] data = ByteBuffer.allocate(4).putInt(exitId).array();
        ReceivePacketType receivePacketType = ReceivePacketType.ESCAPE;
        PacketHeader packetHeader = new PacketHeader(receivePacketType.value, data.length, System.currentTimeMillis());
        Packet packet = new Packet(packetHeader, data);

        send(packet);
    }
}
