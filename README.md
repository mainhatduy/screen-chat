# ScreenChat - Screen OCR for Fedora (GNOME/Wayland)

**ScreenChat** is a lightweight, high-precision screen capture and text extraction (OCR) utility designed specifically for the **Fedora Linux (GNOME/Wayland)** environment. The application is written in **Python/PyQt6** and integrates Google's advanced AI model via the **Gemini API** for fast OCR processing.

The application adheres to an **Industrial Design** styling (minimalist, high contrast, sharp edges) and runs silently directly in the system tray of the operating system.

---

## ✨ Key Features

*   **XDG Desktop Portal Integration**: Works perfectly on GNOME's highly secure Wayland environment by using the system's area selection screenshot tool via DBus.
*   **Daemon & IPC Socket**: Runs in the background in the System Tray and communicates via a Unix Domain Socket to receive screen capture commands from external inputs.
*   **Text Extraction using Gemini API**: Utilizes the latest `google-genai` library with the `gemini-3.1-flash-lite` model for precise OCR of multilingual text, formulas, and code.
*   **Smart Clipboard Workflows**:
    *   **Double Check (Enabled)**: Displays a Review window allowing you to edit the text before copying it.
    *   **Double Check (Disabled)**: Automatically copies the text directly to the system clipboard and displays a professional slide-in toast notification at the corner of the screen.
*   **Flexible Configuration**: Easily change the API Key and customize system prompts (translation, table formatting, code extraction).

---

## 🛠️ System Requirements

*   **Operating System**: Fedora Linux (optimized for Fedora 44 running GNOME & Wayland).
*   **Package Manager**: `uv` (a fast Rust-based Python package manager for quick installation and execution).
*   **API Key**: An API Key from [Google AI Studio](https://aistudio.google.com/) (free).

---

## 📥 Installation & Setup Instructions

### Step 1: Install `uv` (if not already installed)
If you do not have `uv` installed, run the following command for a quick install:
```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```
Or install it via dnf on Fedora:
```bash
sudo dnf install uv
```

### Step 2: Clone the Project and Sync the Environment
Run the following commands to clone the source code and automatically create the virtual environment (`.venv`) along with all required dependencies:
```bash
# Navigate to the project directory
cd screen-chat

# Create virtual environment and install dependencies automatically
uv sync
```

---

## 🚀 How to Use & Configure Shortcuts

### 1. Start the daemon (runs in background)
Run the following command to start the ScreenChat daemon process. The app icon will appear in the System Tray:
```bash
uv run screenchat start
```
*Note:* On the first run, if the API Key is not yet configured, the **Settings** window will display automatically for you to enter your Gemini API Key.

### 2. Configure global shortcut (`Ctrl + Shift + T`) on GNOME
Because Wayland's security model does not allow background applications to register system-wide hotkeys, you must register the shortcut through GNOME Settings:

1.  Open the **Settings** app in Fedora GNOME.
2.  Navigate to **Keyboard** -> **Keyboard Shortcuts**.
3.  Select **View and Customize Shortcuts** -> scroll to the bottom and select **Custom Shortcuts**.
4.  Click **Add Shortcut** (the `+` sign) and fill in the details:
    *   **Name**: `ScreenChat Capture`
    *   **Command**: Enter the absolute path to the executable within the project's virtual environment. For example:
        ```text
        /home/your_username/Workspace/projects/screen-chat/.venv/bin/screenchat capture
        ```
    *   **Shortcut**: Click and press your desired key combination (e.g., `Ctrl + Shift + T`).
5.  Click **Add**.

Now, even when ScreenChat is minimized to the System Tray, pressing your configured shortcut will trigger the GNOME screen capture tool to select a region and automatically pass the text content to the API.

---

## ⚙️ Configuration File (Settings)

Your configuration is stored securely in JSON format at:
`~/.config/screenchat/settings.json`

The file structure is as follows:
```json
{
    "api_key": "YOUR_GEMINI_API_KEY",
    "enable_double_check": true,
    "custom_prompts": [
        {
            "name": "Default OCR",
            "text": "Extract all text from the image exactly as it appears..."
        },
        {
            "name": "Translate to English",
            "text": "Extract all text from the image and translate it to English..."
        }
    ],
    "selected_prompt_name": "Default OCR"
}
```

---

## 📂 Source Code Directory Structure

```text
screen-chat/
├── pyproject.toml         # Project settings & dependencies (managed by uv)
├── README.md              # Documentation and user guide
├── src/
│   └── screenchat/
│       ├── __init__.py
│       ├── main.py        # CLI Entry point (start, capture)
│       ├── core/
│       │   ├── config.py  # settings.json config file management
│       │   ├── gemini.py  # Gemini API integration (google-genai)
│       │   ├── ipc.py     # Inter-process communication socket
│       │   └── wayland.py # Screenshot capture via XDG Desktop Portal (DBus)
│       └── ui/
│           ├── settings.py# API Key & Prompts settings interface
│           ├── review.py  # OCR results preview and edit dialog
│           ├── toast.py   # Slide-in screen notification (Toast)
│           ├── tray.py    # System Tray icon controller
│           └── style.qss  # Industrial Design style sheet (CSS)
```

---

## 📝 License
This project is distributed under the open-source MIT License.
