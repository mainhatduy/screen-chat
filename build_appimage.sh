#!/bin/bash
set -e

# Define directories
BUILD_DIR="build"
DIST_DIR="${BUILD_DIR}/dist"
APPDIR="${BUILD_DIR}/AppDir"
WORK_DIR="${BUILD_DIR}/pyinstaller_work"
LINUXDEPLOY="${BUILD_DIR}/linuxdeploy-x86_64.AppImage"
SVG_ICON="${BUILD_DIR}/screenchat.svg"

# 1. Activate the virtual environment
echo "Activating virtual environment..."
source .venv/bin/activate

# 2. Install PyInstaller in the virtual environment
echo "Installing PyInstaller..."
uv pip install pyinstaller

# 3. Clean up previous build directories if any
echo "Cleaning up previous builds..."
# Clean up legacy root-level folders if they exist
rm -rf dist AppDir screenchat.svg screenchat.spec
# Clean up temporary build-related items (keep downloaded linuxdeploy if it exists)
rm -rf "${WORK_DIR}" "${DIST_DIR}" "${APPDIR}" "${SVG_ICON}" "${BUILD_DIR}/screenchat.spec"

# Ensure build directories exist
mkdir -p "${BUILD_DIR}"
mkdir -p "${APPDIR}/usr/bin"

# 4. Run PyInstaller
echo "Running PyInstaller..."
pyinstaller --name=screenchat \
            --windowed \
            --workpath "${WORK_DIR}" \
            --distpath "${DIST_DIR}" \
            --specpath "${BUILD_DIR}" \
            --add-data "$(pwd)/src/screenchat/ui/style.qss:screenchat/ui" \
            --add-data "$(pwd)/src/screenchat/resources/:screenchat/resources" \
            run_app.py

# 5. Prepare AppDir structure
echo "Preparing AppDir..."
# Copy PyInstaller distribution contents to AppDir/usr/bin
cp -r "${DIST_DIR}/screenchat/"* "${APPDIR}/usr/bin/"

# 6. Download linuxdeploy if not present
if [ ! -f "${LINUXDEPLOY}" ]; then
    echo "Downloading linuxdeploy..."
    curl -sLo "${LINUXDEPLOY}" https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage
    chmod +x "${LINUXDEPLOY}"
fi

# 7. Package as AppImage
echo "Packaging AppImage..."
export VERSION=1.0.0
export ARCH=x86_64

# Copy logo.svg to the build directory so linuxdeploy matches Icon=screenchat in screenchat.desktop
cp src/screenchat/resources/logo.svg "${SVG_ICON}"

# Run linuxdeploy to generate the AppImage
"${LINUXDEPLOY}" \
    --appdir "${APPDIR}" \
    --executable "${APPDIR}/usr/bin/screenchat" \
    --desktop-file screenchat.desktop \
    --icon-file "${SVG_ICON}" \
    --output appimage

echo "AppImage packaging complete! Generated file is in the root directory."
