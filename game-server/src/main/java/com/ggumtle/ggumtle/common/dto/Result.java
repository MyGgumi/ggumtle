package com.ggumtle.ggumtle.common.dto;

import java.nio.charset.Charset;

public interface Result {
    byte[] toBytes(Charset charsets);
}
