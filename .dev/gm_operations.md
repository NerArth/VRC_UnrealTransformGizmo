# Repository Development
This document is not meant to be used by end-users. It serves only to assist repository maintainers with common tasks.

## Useful Graphics Magick Operations
If you have [Graphics Magick](http://www.graphicsmagick.org/download.html) installed, you can use commands to quickly downscale your icon or to crop to foreground content bounds and trim with a transparent background.

Replace variables in the commands with your own file names and parameters.

### Downscaling
768->512/256/128px presets.
``` powershell
gm.exe convert "UEGizmoIcon_Transparent768px.png" -filter point -resize 512x512 "UEGizmoIcon_Transparent512px.png"
gm.exe convert "UEGizmoIcon_Transparent768px.png" -filter point -resize 256x256 "UEGizmoIcon_Transparent256px.png"
gm.exe convert "UEGizmoIcon_Transparent768px.png" -filter point -resize 128x128 "UEGizmoIcon_Transparent128px.png"
```

### Crop to Foreground + Transparent
``` powershell
gm.exe convert "UEGizmoIcon.png" -fuzz 10% -transparent "#232323" -trim +repage -background transparent -gravity center -extent 768x768 "UEGizmoIcon_Square.png"
```