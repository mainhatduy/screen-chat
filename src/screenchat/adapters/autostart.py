import os
import shutil
from pathlib import Path
from loguru import logger

AUTOSTART_DIR = Path.home() / ".config" / "autostart"
AUTOSTART_FILE = AUTOSTART_DIR / "screenchat.desktop"

DESKTOP_ENTRY_TEMPLATE = """\
[Desktop Entry]
Type=Application
Name=ScreenChat
Comment=Screen OCR utility for Fedora GNOME/Wayland
Exec={exec_cmd}
Icon=screenchat
Categories=Utility;
Terminal=false
X-GNOME-Autostart-enabled=true
"""


def _find_exec_cmd() -> str:
    """Resolve the best available command to launch screenchat."""
    # 1. Check if installed as a package script (e.g. via pip/uv install)
    screenchat_bin = shutil.which("screenchat")
    if screenchat_bin:
        return screenchat_bin

    # 2. Fallback: use the python executable that is running this process
    import sys
    return f"{sys.executable} -m screenchat.main"


class AutostartManager:
    """Manages the XDG autostart entry for ScreenChat on Linux desktops."""

    def is_enabled(self) -> bool:
        """Return True if the autostart desktop file exists."""
        return AUTOSTART_FILE.exists()

    def enable(self) -> None:
        """Create the autostart .desktop file."""
        try:
            AUTOSTART_DIR.mkdir(parents=True, exist_ok=True)
            exec_cmd = _find_exec_cmd()
            content = DESKTOP_ENTRY_TEMPLATE.format(exec_cmd=exec_cmd)
            AUTOSTART_FILE.write_text(content, encoding="utf-8")
            logger.info(f"Autostart enabled: {AUTOSTART_FILE} (Exec={exec_cmd})")
        except Exception as e:
            logger.error(f"Failed to enable autostart: {e}")
            raise

    def disable(self) -> None:
        """Remove the autostart .desktop file if it exists."""
        try:
            if AUTOSTART_FILE.exists():
                AUTOSTART_FILE.unlink()
                logger.info(f"Autostart disabled: {AUTOSTART_FILE} removed.")
            else:
                logger.debug("Autostart file not found; nothing to remove.")
        except Exception as e:
            logger.error(f"Failed to disable autostart: {e}")
            raise

    def set_enabled(self, enabled: bool) -> None:
        """Enable or disable autostart based on the boolean flag."""
        if enabled:
            self.enable()
        else:
            self.disable()
