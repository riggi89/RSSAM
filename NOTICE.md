# RSSAM notices

## RSSAM modifications

**RSSAM – Riggi's Steam Achievement Manager** is an independently maintained Windows adaptation of Steam Achievement Manager.

Modifications, extensions, user-interface work, integration work, documentation and release infrastructure:

Copyright © 2026 **Daniel Riggi (riggi89)**.

RSSAM is a modified source version. It is not presented as the original Steam Achievement Manager and is not affiliated with or endorsed by Valve Corporation.

## Original project

RSSAM is derived from **Steam Achievement Manager (SAM)** by **Rick (Gibbed)**.

- Original author: Rick (Gibbed)
- Original project: https://github.com/gibbed/SteamAchievementManager
- Original license: zlib License
- Original copyright: Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)

The original native Steam integration and portions of the achievement and statistic schema logic were retained and adapted for RSSAM. Files copied from or substantially derived from the original project retain attribution headers where applicable.

## CardIdler integration

RSSAM 2.0 includes an adapted version of **CardIdler 1.0.0** by **Sam-218** as the separate `RSSAM.CardIdler` module.

- Original author: Sam-218
- Original component: CardIdler
- License: MIT License
- Original copyright: Copyright (c) 2026 Sam-218

The original WPF window was replaced with an embedded WinUI 3 page and the storage, localization, packaging and lifecycle behavior were integrated into RSSAM. The original MIT license is retained in `src/RSSAM.CardIdler/LICENSE.CardIdler.txt` and reproduced in `LICENSE.md`.

## Third-party dependencies

RSSAM uses **WinUI.TableView 1.4.1** for the table presentation of the game library.

- Project: https://github.com/w-ahmad/WinUI.TableView
- License: MIT License

Third-party components remain subject to their own copyright notices and license terms.

`RSSAM.CardIdler` uses **SteamKit2 3.4.0** to authenticate with the Steam network and report played games.

- Project: https://github.com/SteamRE/SteamKit
- License: GNU Lesser General Public License v2.1

SteamKit2 and its transitive dependencies remain subject to their own package notices and license terms.

`RSSAM.CardIdler` uses **QRCoder 1.8.0** to render Steam authentication challenge URLs as QR images locally.

- Project: https://github.com/Shane32/QRCoder
- License: MIT License
- Original author: Raffael Herrmann

QRCoder remains subject to its own copyright notice and MIT license terms.

## License information

The complete fork copyright, original-project copyright, modification notice, zlib license and third-party notice are provided in [`LICENSE.md`](LICENSE.md).

Technical project documentation is maintained under [`docs`](docs/README.md). Release history is maintained separately in [`CHANGELOG.md`](CHANGELOG.md).
