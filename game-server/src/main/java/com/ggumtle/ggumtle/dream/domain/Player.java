package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.Getter;

@Getter
public class Player {
    private static final int POSITION_BUFFER_SIZE = 500;

    protected long id;

    protected Position[] positions = new Position[POSITION_BUFFER_SIZE];
    private int head = 0;
    private int tail = 0;

    public Player(long id, Position position) {
        this.id = id;
        this.addPosition(position);
    }

    public synchronized void addPosition(Position position) {
        positions[tail] = position;
        tail = (tail + 1) % POSITION_BUFFER_SIZE;

        if (tail == head) {
            head = (head + 1) % POSITION_BUFFER_SIZE;
        }
    }

    public synchronized boolean isEmpty() {
        return head == tail && positions[head] == null;
    }
}
