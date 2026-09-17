# RSSAM application icon assets

The RSSAM application icon combines a game controller with an achievement medal. It was created for the RSSAM fork in 2026 and does not contain the Steam or Valve logo.

Icon artwork and integration: Copyright (c) 2026 Daniel Riggi (riggi89).

## Source files

The files are stored under `src/RSSAM.App/Assets`.

| File | Purpose |
| --- | --- |
| `RSSAM-AppIcon.png` | 1024-pixel master PNG |
| `RSSAM-AppIcon-16.png` through `RSSAM-AppIcon-256.png` | Prepared transparent PNG sizes |
| `RSSAM-AppIcon-24.png` | Custom title-bar icon deployed with the application |
| `RSSAM-AppIcon.ico` | Multi-resolution Windows executable and installer icon |

The ICO contains 16, 24, 32, 48, 64, 128 and 256-pixel images.

## Integration

- `RSSAM-AppIcon.ico` is configured as the application icon and used by the installer and Windows shortcuts.
- `RSSAM-AppIcon-24.png` is copied to publish output for the custom title bar.
- `RSSAM-AppIcon-256.png` is used by the repository application README.

The assets are distributed under the project license. See [`LICENSE.md`](../../LICENSE.md) and [`NOTICE.md`](../../NOTICE.md).
