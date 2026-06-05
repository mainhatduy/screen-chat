from PyQt6.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QTextEdit, QPushButton
from PyQt6.QtCore import Qt
import pyperclip

class ReviewWindow(QDialog):
    def __init__(self, text_content: str):
        super().__init__()
        self.setWindowTitle("ScreenChat - Review OCR")
        self.resize(600, 400)
        # Always on top so user can copy easily
        self.setWindowFlags(self.windowFlags() | Qt.WindowType.WindowStaysOnTopHint & ~Qt.WindowType.WindowContextHelpButtonHint)

        self.text_content = text_content
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout()
        
        self.text_editor = QTextEdit()
        self.text_editor.setPlainText(self.text_content)
        layout.addWidget(self.text_editor)

        btn_layout = QHBoxLayout()
        self.copy_btn = QPushButton("COPY TO CLIPBOARD")
        self.copy_btn.setObjectName("primaryAction")
        self.copy_btn.clicked.connect(self.copy_and_close)

        self.close_btn = QPushButton("DISCARD")
        self.close_btn.clicked.connect(self.reject)

        btn_layout.addStretch()
        btn_layout.addWidget(self.close_btn)
        btn_layout.addWidget(self.copy_btn)

        layout.addLayout(btn_layout)
        self.setLayout(layout)

    def copy_and_close(self):
        edited_text = self.text_editor.toPlainText()
        pyperclip.copy(edited_text)
        self.accept()
