# Create Unity Package Automation

This folder contains scripts to automate the creation of Unity packages using the `create-unity-package` tool.

## Tool Information

- **Repository**: [ShiJbey/create_unity_package](https://github.com/ShiJbey/create_unity_package)
- **Description**: A CLI tool for generating boilerplate Unity package structures or preparing releases.

## Setup Instructions

1. Run `setup_venv.ps1` to create a local Python virtual environment in `.dev/.venv` and install the necessary dependencies.
   ```powershell
   .\setup_venv.ps1
   ```

## Usage

1. Run `create_package.ps1` to prepare a release.
   ```powershell
   .\create_package.ps1
   ```

Refer to the [original README](https://github.com/ShiJbey/create_unity_package/blob/main/README.md) for detailed tool usage and options.

## VPM (VRChat Package Manager) / VCC Setup

The automation supports generating a VPM-compatible repository index for the VRChat Creator Companion.

### Hosting your own VCC Repo

1.  **Tag**: Ensure your `package.json` version is updated.
2.  **Build**: Run `.\.dev\create_package.ps1`. This creates a `.zip` in `.dev/Releases/`.
3.  **GitHub Release**:
    - Push your changes and create a Git Tag (e.g., `v1.0.1`).
    - Create a GitHub Release for that tag.
    - Upload the ZIP file from `.dev/Releases/` to the release.
4.  **VPM Index**: The script automatically updates `docs/index.json` to point to the GitHub download link.
5.  **Add to VCC**: Once you push the `docs/` folder, users can add your repo using:
    `vcc://vpm/addRepo?url=https://nerarth.github.io/VRC_UnrealTransformGizmo/index.json`

### Updating your Repo Information
You can edit `.dev/vpm_repo_manager.py` to change the `REPO_NAME` or `REPO_AUTHOR`.
