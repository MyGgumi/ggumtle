package com.ggumtle.designsystem.component

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.theme.GameColors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun GameSearchBar(
    query: TextFieldValue,
    onQueryChange: (TextFieldValue) -> Unit,
    placeholder: String = "검색...",
    modifier: Modifier = Modifier,
) {
    OutlinedTextField(
        value = query,
        onValueChange = onQueryChange,
        placeholder = {
            Text(
                placeholder,
                color = GameColors.textSecondary
            )
        },
        leadingIcon = {
            Icon(
                Icons.Default.Search,
                contentDescription = "검색",
                tint = GameColors.primary,
                modifier = Modifier.size(24.dp)
            )
        },
        colors = OutlinedTextFieldDefaults.colors(
            focusedContainerColor = GameColors.surface,
            unfocusedContainerColor = GameColors.surface,
            focusedBorderColor = GameColors.primary,
            unfocusedBorderColor = Color.Transparent,
            focusedTextColor = GameColors.textPrimary,
            unfocusedTextColor = GameColors.textPrimary
        ),
        shape = RoundedCornerShape(16.dp),
        singleLine = true,
        modifier = modifier.fillMaxWidth()
    )
}

@Composable
fun GameSearchBarClickable(
    placeholder: String = "검색...",
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    GameCard(
        onClick = onClick,
        modifier = modifier
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 12.dp, vertical = 16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                Icons.Default.Search,
                contentDescription = "검색",
                tint = GameColors.primary,
                modifier = Modifier.size(24.dp)
            )
            Spacer(modifier = Modifier.width(12.dp))
            Text(
                text = placeholder,
                color = GameColors.textSecondary,
                fontSize = 16.sp
            )
        }
    }
}