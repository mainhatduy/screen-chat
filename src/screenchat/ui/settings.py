from PyQt6.QtWidgets import QHBoxLayout, QVBoxLayout, QLabel, QLineEdit, QPushButton, QCheckBox, QComboBox, QMessageBox, QListView, QStyledItemDelegate
from PyQt6.QtCore import Qt
from ..application.use_cases import SettingsUseCase
from ..adapters.autostart import AutostartManager
from .base_dialog import ModernDialog

class SettingsWindow(ModernDialog):
    def __init__(self, settings_use_case: SettingsUseCase):
        super().__init__(title="ScreenChat - Settings", stays_on_top=False)
        self.settings_use_case = settings_use_case
        self.autostart_manager = AutostartManager()
        self.resize(500, 385)
        
        self.init_ui()
        self.load_settings()

    def init_ui(self):
        # API Key
        api_layout = QVBoxLayout()
        api_layout.setSpacing(6)
        api_label = QLabel("GEMINI API KEY:")
        self.api_input = QLineEdit()
        self.api_input.setEchoMode(QLineEdit.EchoMode.Password)
        self.api_input.setPlaceholderText("Enter API Key here...")
        api_layout.addWidget(api_label)
        api_layout.addWidget(self.api_input)
        self.content_layout.addLayout(api_layout)

        # Prompts
        prompt_layout = QVBoxLayout()
        prompt_layout.setSpacing(6)
        prompt_label = QLabel("SYSTEM PROMPT:")
        self.prompt_combo = QComboBox()
        list_view = QListView()
        list_view.setSpacing(4)
        self.prompt_combo.setView(list_view)
        self.prompt_combo.setItemDelegate(QStyledItemDelegate(self.prompt_combo))
        # Set stylesheet on the popup container widget to prevent native styling white lines
        self.prompt_combo.view().parentWidget().setStyleSheet("background-color: #1A1A1E; border: none;")
        settings = self.settings_use_case.get_settings()
        for p in settings.custom_prompts:
            self.prompt_combo.addItem(p.name, p.text)
        prompt_layout.addWidget(prompt_label)
        prompt_layout.addWidget(self.prompt_combo)
        self.content_layout.addLayout(prompt_layout)

        # Double Check
        self.double_check_cb = QCheckBox("Enable Double Check (Preview before copying)")
        self.content_layout.addWidget(self.double_check_cb)

        # Autostart
        self.autostart_cb = QCheckBox("Launch ScreenChat at system startup")
        self.content_layout.addWidget(self.autostart_cb)

        # Spacer
        self.content_layout.addStretch()

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
        
        self.content_layout.addLayout(btn_layout)


    def load_settings(self):
        settings = self.settings_use_case.get_settings()
        self.api_input.setText(settings.api_key)
        self.double_check_cb.setChecked(settings.enable_double_check)
        idx = self.prompt_combo.findText(settings.selected_prompt_name)
        if idx >= 0:
            self.prompt_combo.setCurrentIndex(idx)
        self.autostart_cb.setChecked(self.autostart_manager.is_enabled())

    def save_settings(self):
        api_key = self.api_input.text().strip()
        if not api_key:
            QMessageBox.warning(self, "Error", "API Key cannot be empty!")
            return

        self.settings_use_case.save_settings(
            api_key=api_key,
            enable_double_check=self.double_check_cb.isChecked(),
            selected_prompt_name=self.prompt_combo.currentText()
        )

        try:
            self.autostart_manager.set_enabled(self.autostart_cb.isChecked())
        except Exception as e:
            QMessageBox.warning(self, "Autostart Error", f"Could not update autostart setting:\n{e}")

        self.accept()
