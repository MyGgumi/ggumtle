package ggumtle.ggumtle.dreamtest.event;

import lombok.extern.slf4j.Slf4j;

import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;

@Slf4j
public record InitializeMapEvent(
        List<Chest> chests,
        List<Ggumtle> ggumtles,
        List<HealPack> healPacks,
        List<SpeedPack> speedPacks
) {
    public static InitializeMapEvent of(byte[] bytes) {
        try {
            // 바이너리 데이터를 바이트 배열로 변환
            ByteBuffer buffer = ByteBuffer.wrap(bytes);
            
            List<Chest> chests = new ArrayList<>();
            List<Ggumtle> ggumtles = new ArrayList<>();
            List<HealPack> healPacks = new ArrayList<>();
            List<SpeedPack> speedPacks = new ArrayList<>();
            
            // 상자(Box -> Chest) 정보 파싱
            int chestCount = buffer.getInt();
            for (int i = 0; i < chestCount; i++) {
                int id = buffer.getInt();
                int x = buffer.getInt();
                int y = buffer.getInt();
                int z = buffer.getInt();
                chests.add(new Chest(id, x, y, z));
            }
            
            // 꿈틀이 정보 파싱
            int ggumtleCount = buffer.getInt();
            for (int i = 0; i < ggumtleCount; i++) {
                int id = buffer.getInt();
                int x = buffer.getInt();
                int y = buffer.getInt();
                int z = buffer.getInt();
                ggumtles.add(new Ggumtle(id, x, y, z));
            }
            
            // 힐팩 정보 파싱
            int healPackCount = buffer.getInt();
            for (int i = 0; i < healPackCount; i++) {
                int id = buffer.getInt();
                int x = buffer.getInt();
                int y = buffer.getInt();
                int z = buffer.getInt();
                healPacks.add(new HealPack(id, x, y, z));
            }
            
            // 스피드팩 정보 파싱
            int speedPackCount = buffer.getInt();
            for (int i = 0; i < speedPackCount; i++) {
                int id = buffer.getInt();
                int x = buffer.getInt();
                int y = buffer.getInt();
                int z = buffer.getInt();
                speedPacks.add(new SpeedPack(id, x, y, z));
            }
            
            log.info("맵 초기화 파싱 완료: 상자={}개, 꿈틀이={}개, 힐팩={}개, 스피드팩={}개", 
                    chestCount, ggumtleCount, healPackCount, speedPackCount);
            
            return new InitializeMapEvent(chests, ggumtles, healPacks, speedPacks);
            
        } catch (Exception e) {
            log.error("InitializeMapEvent 파싱 실패", e);
            return new InitializeMapEvent(
                    new ArrayList<>(), 
                    new ArrayList<>(), 
                    new ArrayList<>(), 
                    new ArrayList<>()
            );
        }
    }
    
    /**
     * 상자 정보 (Box -> Chest로 명명)
     */
    public record Chest(int id, int x, int y, int z) {
        @Override
        public String toString() {
            return String.format("Chest{id=%d, pos=(%d,%d,%d)}", id, x, y, z);
        }
    }
    
    public record Ggumtle(int id, int x, int y, int z) {
        @Override
        public String toString() {
            return String.format("Ggumtle{id=%d, pos=(%d,%d,%d)}", id, x, y, z);
        }
    }
    
    public record HealPack(int id, int x, int y, int z) {
        @Override
        public String toString() {
            return String.format("HealPack{id=%d, pos=(%d,%d,%d)}", id, x, y, z);
        }
    }
    
    public record SpeedPack(int id, int x, int y, int z) {
        @Override
        public String toString() {
            return String.format("SpeedPack{id=%d, pos=(%d,%d,%d)}", id, x, y, z);
        }
    }
}
