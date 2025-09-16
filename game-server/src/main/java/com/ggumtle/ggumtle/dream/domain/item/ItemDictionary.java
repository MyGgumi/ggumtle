package com.ggumtle.ggumtle.dream.domain.item;

import lombok.AllArgsConstructor;

@AllArgsConstructor
public enum ItemDictionary {
    LIGHT_JELLY(1, new LightJelly(1, 60, 6, 99)),
    FLASH(2, new Flash(2, 3, 1, 3)),
    TASER(3, new Taser(3, 3, 1, 3)),
    ;

    public final int id;
    public final Boxable boxableItem;

    public static Boxable valueOf(int id) {
        for (ItemDictionary itemDictionary : values()) {
            if (itemDictionary.id == id) {
                return itemDictionary.boxableItem;
            }
        }

        return null;
    }
}
