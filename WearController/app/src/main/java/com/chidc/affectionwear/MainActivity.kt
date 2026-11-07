package com.chidc.affectionwear

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import com.chidc.affectionwear.ui.AffectionApp
import com.chidc.affectionwear.ui.theme.AffectionTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            AffectionTheme {
                AffectionApp()
            }
        }
    }
}
