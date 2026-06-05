from PyQt6.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, QPushButton, QCheckBox, QComboBox, QMessageBox
from PyQt6.QtCore import Qt
from ..core.config import ConfigManager

class SettingsWindow(QDialog):
    def __init__(self, config_manager: ConfigManager):
        super().__init__()
        self.config_manager = config_manager
        self.setWindowTitle("ScreenChat - Settings")
        self.setFixedSize(500, 300)
        self.setWindowFlags(self.windowFlags() & ~Qt.WindowType.WindowContextHelpButtonHint)
        
        self.init_ui()
        self.load_settings()

    def init_ui(self):
        layout = QVBoxLayout()
        layout.setSpacing(15)

        # API Key
        api_layout = QVBoxLayout()
        api_label = QLabel("GEMINI API KEY:")
        api_label.setStyleSheet("font-weight: bold; color: #8A8A8A;")
        self.api_input = QLineEdit()
        self.api_input.setEchoMode(QLineEdit.EchoMode.Password)
        self.api_input.setPlaceholderText("Enter API Key here...")
        api_layout.addWidget(api_label)
        api_layout.addWidget(self.api_input)
        layout.addLayout(api_layout)

        # Prompts
        prompt_layout = QVBoxLayout()
        prompt_label = QLabel("SYSTEM PROMPT:")
        prompt_label.setStyleSheet("font-weight: bold; color: #8A8A8A;")
        self.prompt_combo = QComboBox()
        for p in self.config_manager.settings.custom_prompts:
            self.prompt_combo.addItem(p.name, p.text)
        prompt_layout.addWidget(prompt_label)
        prompt_layout.addWidget(self.prompt_combo)
        layout.addLayout(prompt_layout)

        # Double Check
        self.double_check_cb = QCheckBox("Enable Double Check (Preview before copying)")
        layout.addWidget(self.double_check_cb)

        # Spacer
        layout.addStretch()

        # Buttons
        btn_layout = QHBoxLayout()
        self.save_btn = QPushButton("SAVE SETTINGS")
        self.save_btn.setObjectName("primaryAction")
        self.save_btn.clicked.connect(self.save_settings)
        
        self.cancel_btn = QPushButton("CANCEL")
        self.cancel_btn.clicked.connect(self.reject)

        btn_layout.addStretch()
        btn_layout.addWidget(self.cancel_btn)
        btn_layout.addWidget(self.save_btn)
        
        layout.addLayout(btn_layout)
        self.setLayout(layout)

    def load_settings(self):
        self.api_input.setText(self.config_manager.settings.api_key)
        self.double_check_cb.setChecked(self.config_manager.settings.enable_double_check)
        idx = self.prompt_combo.findText(self.config_manager.settings.selected_prompt_name)
        if idx >= 0:
            self.prompt_combo.setCurrentIndex(idx)

    def save_settings(self):
        api_key = self.api_input.text().strip()
        if not api_key:
            QMessageBox.warning(self, "Error", "API Key cannot be empty!")
            return

        self.config_manager.settings.api_key = api_key
        self.config_manager.settings.enable_double_check = self.double_check_cb.isChecked()
        self.config_manager.settings.selected_prompt_name = self.prompt_combo.currentText()
        
        self.config_manager.save()
        self.accept()
