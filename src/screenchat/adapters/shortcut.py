import os
import shutil
import subprocess
import ast
import sys
from loguru import logger

def _find_exec_cmd() -> str:
    """Resolve the best available command to launch screenchat."""
    screenchat_bin = shutil.which("screenchat")
    if screenchat_bin:
        return screenchat_bin
    return f"{sys.executable} -m screenchat.main"

def qt_to_gnome_shortcut(qt_shortcut: str) -> str:
    # Example: "Ctrl+Alt+S" -> "<Ctrl><Alt>s"
    parts = qt_shortcut.split("+")
    gnome_modifiers = []
    key = ""
    for part in parts:
        part_lower = part.strip().lower()
        if part_lower == "ctrl":
            gnome_modifiers.append("<Ctrl>")
        elif part_lower == "alt":
            gnome_modifiers.append("<Alt>")
        elif part_lower == "shift":
            gnome_modifiers.append("<Shift>")
        elif part_lower in ("meta", "win", "super"):
            gnome_modifiers.append("<Super>")
        else:
            key = part_lower
            
    special_keys = {
        "esc": "Escape",
        "space": "space",
        "enter": "Return",
        "return": "Return",
        "tab": "Tab",
        "backspace": "BackSpace",
        "delete": "Delete",
        "insert": "Insert",
        "home": "Home",
        "end": "End",
        "pageup": "Page_Up",
        "pagedown": "Page_Down",
        "up": "Up",
        "down": "Down",
        "left": "Left",
        "right": "Right",
        "printscreen": "Print",
    }
    key = special_keys.get(key, key)
    return "".join(gnome_modifiers) + key

def gnome_to_qt_shortcut(gnome_shortcut: str) -> str:
    # Example: "<Shift><Super>t" -> "Shift+Meta+T"
    gnome_shortcut = gnome_shortcut.strip("'\"")
    parts = []
    rem = gnome_shortcut
    while rem.startswith("<"):
        end_idx = rem.find(">")
        if end_idx == -1:
            break
        mod = rem[1:end_idx].lower()
        rem = rem[end_idx+1:]
        
        if mod == "ctrl" or mod == "primary":
            parts.append("Ctrl")
        elif mod == "alt":
            parts.append("Alt")
        elif mod == "shift":
            parts.append("Shift")
        elif mod in ("super", "meta"):
            parts.append("Meta")
            
    key = rem
    if key:
        special_keys = {
            "escape": "Esc",
            "space": "Space",
            "return": "Enter",
            "tab": "Tab",
            "backspace": "Backspace",
            "delete": "Delete",
            "insert": "Insert",
            "home": "Home",
            "end": "End",
            "page_up": "PageUp",
            "page_down": "PageDown",
            "up": "Up",
            "down": "Down",
            "left": "Left",
            "right": "Right",
            "print": "PrintScreen",
        }
        key = special_keys.get(key.lower(), key.upper())
        parts.append(key)
        
    return "+".join(parts)

class ShortcutManager:
    def __init__(self):
        self.is_gnome = False
        # Check if gsettings is available
        if shutil.which("gsettings"):
            # Also check if we are on GNOME
            desktop = os.environ.get("XDG_CURRENT_DESKTOP", "").upper()
            if "GNOME" in desktop or "UNITY" in desktop:
                self.is_gnome = True
                
    def _run_gsettings(self, args: list[str]) -> str:
        cmd = ["gsettings"] + args
        res = subprocess.run(cmd, capture_output=True, text=True, check=True)
        return res.stdout.strip()

    def get_custom_bindings_list(self) -> list[str]:
        if not self.is_gnome:
            return []
        try:
            out = self._run_gsettings(["get", "org.gnome.settings-daemon.plugins.media-keys", "custom-keybindings"])
            if out.startswith("@as"):
                out = out[3:].strip()
            if not out or out == "[]":
                return []
            return ast.literal_eval(out)
        except Exception as e:
            logger.warning(f"Failed to read custom bindings list: {e}")
            return []

    def get_shortcut(self, default_val: str = "Shift+Meta+T") -> str:
        if not self.is_gnome:
            return default_val
        try:
            paths = self.get_custom_bindings_list()
            for path in paths:
                try:
                    name = self._run_gsettings(["get", f"org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path}", "name"])
                    command = self._run_gsettings(["get", f"org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path}", "command"])
                    binding = self._run_gsettings(["get", f"org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path}", "binding"])
                    
                    name = name.strip("'\"")
                    command = command.strip("'\"")
                    binding = binding.strip("'\"")
                    
                    if name == "ScreenChat" or "screenchat capture" in command:
                        return gnome_to_qt_shortcut(binding)
                except Exception:
                    continue
        except Exception as e:
            logger.warning(f"Error checking GNOME shortcuts: {e}")
        return default_val

    def set_shortcut(self, qt_shortcut: str) -> None:
        if not self.is_gnome:
            logger.info("Not on GNOME; skipping custom keyboard shortcut registration.")
            return
            
        gnome_binding = qt_to_gnome_shortcut(qt_shortcut)
        exec_cmd = f"{_find_exec_cmd()} capture"
        
        try:
            paths = self.get_custom_bindings_list()
            target_path = None
            
            # 1. Search for existing ScreenChat custom keybinding
            for path in paths:
                try:
                    name = self._run_gsettings(["get", f"org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path}", "name"]).strip("'\"")
                    command = self._run_gsettings(["get", f"org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path}", "command"]).strip("'\"")
                    if name == "ScreenChat" or "screenchat capture" in command:
                        target_path = path
                        break
                except Exception:
                    continue
            
            # 2. If not found, create a new one
            if not target_path:
                # Find the next free custom{i} index
                index = 0
                while True:
                    candidate = f"/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/custom{index}/"
                    if candidate not in paths:
                        target_path = candidate
                        break
                    index += 1
                
                # Add it to the list
                paths.append(target_path)
                self._run_gsettings(["set", "org.gnome.settings-daemon.plugins.media-keys", "custom-keybindings", str(paths)])
            
            # 3. Configure the shortcut properties
            schema = f"org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{target_path}"
            self._run_gsettings(["set", schema, "name", "ScreenChat"])
            self._run_gsettings(["set", schema, "command", exec_cmd])
            self._run_gsettings(["set", schema, "binding", gnome_binding])
            
            logger.info(f"GNOME keyboard shortcut set to {gnome_binding} (path: {target_path})")
        except Exception as e:
            logger.error(f"Failed to set GNOME keyboard shortcut: {e}")
