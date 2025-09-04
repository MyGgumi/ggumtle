package com.ggumtle.ggumtle;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.ConfigurationPropertiesScan;

@SpringBootApplication
@ConfigurationPropertiesScan
public class GgumtleApplication {

    public static void main(String[] args) {
        SpringApplication.run(GgumtleApplication.class, args);
    }

}
