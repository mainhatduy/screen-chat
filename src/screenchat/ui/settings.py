from PyQt6.QtWidgets import (
    QHBoxLayout, QVBoxLayout, QLabel, QLineEdit, QPushButton,
    QCheckBox, QComboBox, QMessageBox, QListView, QStyledItemDelegate,
    QPlainTextEdit, QInputDialog
)
from PyQt6.QtCore import Qt
from ..application.use_cases import SettingsUseCase
from ..adapters.autostart import AutostartManager
from .base_dialog import ModernDialog
from ..domain.models import CustomPrompt

class SettingsWindow(ModernDialog):
    def __init__(self, settings_use_case: SettingsUseCase):
        super().__init__(title="ScreenChat - Settings", stays_on_top=False)
        self.settings_use_case = settings_use_case
        self.autostart_manager = AutostartManager()
        self.resize(520, 500)
        
        self.local_prompts = []
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
        prompt_layout.addWidget(prompt_label)

        # Combo and buttons container
        prompt_combo_layout = QHBoxLayout()
        prompt_combo_layout.setSpacing(6)

        self.prompt_combo = QComboBox()
        list_view = QListView()
        list_view.setSpacing(4)
        self.prompt_combo.setView(list_view)
        self.prompt_combo.setItemDelegate(QStyledItemDelegate(self.prompt_combo))
        # Set stylesheet on the popup container widget to prevent native styling white lines
        self.prompt_combo.view().parentWidget().setStyleSheet("background-color: #1A1A1E; border: none;")
        
        self.add_prompt_btn = QPushButton("+ NEW")
        self.add_prompt_btn.setStyleSheet("padding: 6px 12px; font-size: 11px;")
        self.add_prompt_btn.clicked.connect(self.add_prompt)

        self.rename_prompt_btn = QPushButton("RENAME")
        self.rename_prompt_btn.setStyleSheet("padding: 6px 12px; font-size: 11px;")
        self.rename_prompt_btn.clicked.connect(self.rename_prompt)

        self.delete_prompt_btn = QPushButton("DELETE")
        self.delete_prompt_btn.setStyleSheet("padding: 6px 12px; font-size: 11px;")
        self.delete_prompt_btn.clicked.connect(self.delete_prompt)

        prompt_combo_layout.addWidget(self.prompt_combo, 1)
        prompt_combo_layout.addWidget(self.add_prompt_btn)
        prompt_combo_layout.addWidget(self.rename_prompt_btn)
        prompt_combo_layout.addWidget(self.delete_prompt_btn)
        prompt_layout.addLayout(prompt_combo_layout)

        # Prompt text editor
        self.prompt_text_edit = QPlainTextEdit()
        self.prompt_text_edit.setPlaceholderText("Enter system prompt text here...")
        self.prompt_text_edit.setMinimumHeight(120)
        self.prompt_text_edit.textChanged.connect(self.on_prompt_text_changed)
        prompt_layout.addWidget(self.prompt_text_edit)

        self.content_layout.addLayout(prompt_layout)
        
        self.prompt_combo.currentIndexChanged.connect(self.on_prompt_index_changed)

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
        
        # Load prompts locally
        self.local_prompts = [CustomPrompt(name=p.name, text=p.text) for p in settings.custom_prompts]
        
        self.prompt_combo.blockSignals(True)
        self.prompt_combo.clear()
        for p in self.local_prompts:
            self.prompt_combo.addItem(p.name, p.text)
        self.prompt_combo.blockSignals(False)
        
        idx = self.prompt_combo.findText(settings.selected_prompt_name)
        if idx >= 0:
            self.prompt_combo.setCurrentIndex(idx)
        else:
            self.prompt_combo.setCurrentIndex(0)
            
        self.on_prompt_index_changed(self.prompt_combo.currentIndex())
        self.autostart_cb.setChecked(self.autostart_manager.is_enabled())

    def save_settings(self):
        api_key = self.api_input.text().strip()
        if not api_key:
            QMessageBox.warning(self, "Error", "API Key cannot be empty!")
            return

        self.settings_use_case.save_settings(
            api_key=api_key,
            enable_double_check=self.double_check_cb.isChecked(),
            selected_prompt_name=self.prompt_combo.currentText(),
            custom_prompts=self.local_prompts
        )

        try:
            self.autostart_manager.set_enabled(self.autostart_cb.isChecked())
        except Exception as e:
            QMessageBox.warning(self, "Autostart Error", f"Could not update autostart setting:\n{e}")

        self.accept()

    def on_prompt_index_changed(self, index: int):
        if index < 0 or index >= len(self.local_prompts):
            self.prompt_text_edit.clear()
            return
        
        self.prompt_text_edit.blockSignals(True)
        self.prompt_text_edit.setPlainText(self.local_prompts[index].text)
        self.prompt_text_edit.blockSignals(False)
        
        self.delete_prompt_btn.setEnabled(len(self.local_prompts) > 1)

    def on_prompt_text_changed(self):
        index = self.prompt_combo.currentIndex()
        if 0 <= index < len(self.local_prompts):
            self.local_prompts[index].text = self.prompt_text_edit.toPlainText()

    def add_prompt(self):
        name, ok = QInputDialog.getText(self, "New Prompt", "Enter prompt name:")
        if not ok or not name.strip():
            return
        name = name.strip()
        
        if any(p.name.lower() == name.lower() for p in self.local_prompts):
            QMessageBox.warning(self, "Error", f"A prompt named '{name}' already exists!")
            return
        
        new_prompt = CustomPrompt(name=name, text="Enter your custom system prompt here...")
        self.local_prompts.append(new_prompt)
        
        self.prompt_combo.blockSignals(True)
        self.prompt_combo.addItem(new_prompt.name, new_prompt.text)
        self.prompt_combo.blockSignals(False)
        
        self.prompt_combo.setCurrentIndex(self.prompt_combo.count() - 1)
        self.on_prompt_index_changed(self.prompt_combo.currentIndex())
        self.prompt_text_edit.setFocus()

    def rename_prompt(self):
        index = self.prompt_combo.currentIndex()
        if index < 0 or index >= len(self.local_prompts):
            return
            
        current_prompt = self.local_prompts[index]
        name, ok = QInputDialog.getText(
            self, "Rename Prompt", f"Rename '{current_prompt.name}' to:", text=current_prompt.name
        )
        if not ok or not name.strip() or name.strip() == current_prompt.name:
            return
        name = name.strip()
        
        if any(i != index and p.name.lower() == name.lower() for i, p in enumerate(self.local_prompts)):
            QMessageBox.warning(self, "Error", f"A prompt named '{name}' already exists!")
            return
            
        current_prompt.name = name
        
        self.prompt_combo.blockSignals(True)
        self.prompt_combo.setItemText(index, name)
        self.prompt_combo.setItemData(index, current_prompt.text)
        self.prompt_combo.blockSignals(False)

    def delete_prompt(self):
        index = self.prompt_combo.currentIndex()
        if index < 0 or index >= len(self.local_prompts):
            return
        if len(self.local_prompts) <= 1:
            QMessageBox.warning(self, "Error", "You must keep at least one system prompt.")
            return
            
        current_prompt = self.local_prompts[index]
        reply = QMessageBox.question(
            self, 
            "Delete Prompt", 
            f"Are you sure you want to delete '{current_prompt.name}'?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
            QMessageBox.StandardButton.No
        )
        if reply == QMessageBox.StandardButton.Yes:
            self.local_prompts.pop(index)
            
            self.prompt_combo.blockSignals(True)
            self.prompt_combo.removeItem(index)
            self.prompt_combo.blockSignals(False)
            
            new_index = index
            if new_index >= self.prompt_combo.count():
                new_index = self.prompt_combo.count() - 1
            self.prompt_combo.setCurrentIndex(new_index)
            self.on_prompt_index_changed(new_index)
