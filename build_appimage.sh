#!/bin/bash
set -e

# 1. Activate the virtual environment
echo "Activating virtual environment..."
source .venv/bin/activate

# 2. Install PyInstaller in the virtual environment
echo "Installing PyInstaller..."
uv pip install pyinstaller

# 3. Clean up previous build directories if any
echo "Cleaning up previous builds..."
rm -rf build dist AppDir *.AppImage screenchat.svg

# 4. Run PyInstaller
# We need to bundle src/screenchat/ui/style.qss and copy it to the correct path
echo "Running PyInstaller..."
pyinstaller --name=screenchat \
            --windowed \
            --add-data "src/screenchat/ui/style.qss:screenchat/ui" \
            --add-data "src/screenchat/resources/:screenchat/resources" \
            run_app.py

# 5. Create AppDir structure
echo "Preparing AppDir..."
mkdir -p AppDir/usr/bin

# Copy PyInstaller distribution contents to AppDir/usr/bin
cp -r dist/screenchat/* AppDir/usr/bin/

# 6. Download linuxdeploy
echo "Downloading linuxdeploy..."
curl -sLo linuxdeploy-x86_64.AppImage https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage
chmod +x linuxdeploy-x86_64.AppImage

# 7. Package as AppImage
echo "Packaging AppImage..."
# Disable type checks and update check to keep linuxdeploy running smoothly in the build environment
export VERSION=1.0.0
export ARCH=x86_64

# Copy logo.svg to screenchat.svg so linuxdeploy matches Icon=screenchat in screenchat.desktop
cp src/screenchat/resources/logo.svg screenchat.svg

# Run linuxdeploy to generate the AppImage
./linuxdeploy-x86_64.AppImage \
    --appdir AppDir \
    --executable AppDir/usr/bin/screenchat \
    --desktop-file screenchat.desktop \
    --icon-file screenchat.svg \
    --output appimage

echo "AppImage packaging complete! Generated file is in the root directory."
