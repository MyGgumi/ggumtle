package com.ggumtle.ggumtle.common.dto;

import java.nio.charset.Charset;

public interface Body {
    byte[] toBytes(Charset charsets);
}
