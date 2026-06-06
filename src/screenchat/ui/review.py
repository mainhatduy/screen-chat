from PyQt6.QtWidgets import QHBoxLayout, QTextEdit, QPushButton
from PyQt6.QtCore import Qt
from .base_dialog import ModernDialog

class ReviewWindow(ModernDialog):
    def __init__(self, text_content: str):
        super().__init__(title="ScreenChat - Review OCR", stays_on_top=True)
        self.resize(600, 420)

        self.text_content = text_content
        self.init_ui()

    def init_ui(self):
        self.text_editor = QTextEdit()
        self.text_editor.setPlainText(self.text_content)
        self.content_layout.addWidget(self.text_editor)

        btn_layout = QHBoxLayout()
        btn_layout.setSpacing(12)
        
        self.close_btn = QPushButton("DISCARD")
        self.close_btn.clicked.connect(self.reject)
        
        self.copy_btn = QPushButton("COPY TO CLIPBOARD")
        self.copy_btn.setObjectName("primaryAction")
        self.copy_btn.clicked.connect(self.accept)

        btn_layout.addStretch()
        btn_layout.addWidget(self.close_btn)
        btn_layout.addWidget(self.copy_btn)

        self.content_layout.addLayout(btn_layout)

    def get_text(self) -> str:
        return self.text_editor.toPlainText()

