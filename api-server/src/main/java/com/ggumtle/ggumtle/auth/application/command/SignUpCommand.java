package com.ggumtle.ggumtle.auth.application.command;

import java.time.LocalDate;

public record SignUpCommand(
        String idToken,
        String name,
        String nickname,
        LocalDate birthDate
) {
}
