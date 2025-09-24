package com.ggumtle.ggumtle.dream.domain;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Getter;
import lombok.ToString;

@Getter
@ToString
public class OptimalServer {
    public static final String KEY = "optimal-server";

    private Long id;

    private String host;

    private int port;

    @JsonCreator
    public OptimalServer(@JsonProperty("id") Long id,
                         @JsonProperty("host") String host,
                         @JsonProperty("port") int port) {
        this.id = id;
        this.host = host;
        this.port = port;
    }
}
