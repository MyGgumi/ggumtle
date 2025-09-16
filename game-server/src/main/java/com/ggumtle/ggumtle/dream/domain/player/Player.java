package com.ggumtle.ggumtle.dream.domain.player;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.Getter;

@Getter
public class Player {
    private static final int POSITION_BUFFER_SIZE = 500;

    protected long id;

    public final int moveSpeed;

    protected Position[] positions = new Position[POSITION_BUFFER_SIZE];
    private int curr = 0;
    private final Object positionLock;

    public Player(long id, Position position, int moveSpeed) {
        this.id = id;
        this.moveSpeed = moveSpeed;
        this.positionLock = new Object();
        this.addPosition(position);
    }

    public void addPosition(Position position) {
        synchronized (this.positionLock) {
            curr = (curr + 1) % POSITION_BUFFER_SIZE;
            positions[curr] = position;
        }
    }

    // TODO: 네트워크 지연 보정
    public Position getPositionAt(long timestamp) {
        synchronized (this.positionLock) {
            Position position = this.positions[curr];
            for (int i = 0; i < POSITION_BUFFER_SIZE; i++) {
                int index = (curr - i + POSITION_BUFFER_SIZE) % POSITION_BUFFER_SIZE;

                if (positions[index] == null) {
                    break;
                }

                if (positions[index].timestamp <= timestamp) {
                    return positions[index];
                }
                position = positions[index];
            }

            return position;
        }
    }

    public Position getLastPosition() {
        synchronized (this.positionLock) {
            return this.positions[curr];
        }
    }
}
