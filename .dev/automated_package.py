import json
import os
import sys
from pathlib import Path

# Add the cloned tool to sys.path so we can import its classes
# This assumes PYTHONPATH is set or we add it here.
# For robustness, we'll add the cup-cloned path relative to this script.
script_dir = Path(__file__).parent
project_root = script_dir.parent
cup_path = project_root / ".venv" / "cup-cloned"
sys.path.append(str(cup_path))

try:
    from create_unity_package.create_unity_package import AuthorInfo, PackageInfo, create_package
except ImportError as e:
    print(f"Error: Could not import create_unity_package. Ensure setup_venv.ps1 has been run. {e}")
    sys.exit(1)

def main():
    package_json_path = project_root / "package.json"
    if not package_json_path.exists():
        print(f"Error: package.json not found at {package_json_path}")
        sys.exit(1)

    with open(package_json_path, "r") as f:
        data = json.load(f)

    # Map package.json to PackageInfo
    author_data = data.get("author", {})
    if isinstance(author_data, str):
        # Handle string author if necessary, though package.json here has an object
        author = AuthorInfo(name=author_data, email="")
    else:
        author = AuthorInfo(
            name=author_data.get("name", "Unknown"),
            email=author_data.get("email", ""),
            url=author_data.get("url", "")
        )

    package_info = PackageInfo(
        name=data.get("name"),
        version=data.get("version"),
        displayName=data.get("displayName"),
        description=data.get("description"),
        unity=data.get("unity", "2022.1"),
        author=author,
        keywords=data.get("keywords", [])
    )

    # Output directory: .dev/Releases/<package_name>
    release_dir = project_root / ".dev" / "Releases" / package_info.name
    if not release_dir.parent.exists():
        os.makedirs(release_dir.parent)

    print(f"Creating package for {package_info.displayName} v{package_info.version}...")
    print(f"Output directory: {release_dir}")

    # The tool's create_package expects the directory to NOT exist yet as it calls os.mkdir
    if release_dir.exists():
        import shutil
        print(f"Warning: {release_dir} already exists. Removing old content...")
        shutil.rmtree(release_dir)

    create_package(release_dir, package_info)

    # Post-process: Replace generated files with root equivalents
    import shutil
    import zipfile
    import hashlib

    print("Replacing generated README and LICENSE with root versions...")
    
    source_readme = project_root / "README.md"
    source_license = project_root / "LICENSE"
    
    if source_readme.exists():
        shutil.copy2(source_readme, release_dir / "README.md")
    
    if source_license.exists():
        # Copy root LICENSE to LICENSE.md in release
        shutil.copy2(source_license, release_dir / "LICENSE.md")

    # Zip the package for VPM
    zip_name = f"{package_info.name}-{package_info.version}.zip"
    zip_path = project_root / ".dev" / "Releases" / zip_name
    
    print(f"Creating VPM zip: {zip_path}")
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(release_dir):
            for file in files:
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, release_dir)
                zipf.write(file_path, arcname)

    # Calculate SHA256 for VPM
    sha256_hash = hashlib.sha256()
    with open(zip_path, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            sha256_hash.update(byte_block)
    
    hash_value = sha256_hash.hexdigest()
    hash_path = zip_path.with_suffix(".zip.sha256")
    with open(hash_path, "w") as f:
        f.write(hash_value)
    
    print(f"SHA256: {hash_value}")
    print("Package creation complete!")

if __name__ == "__main__":
    main()
