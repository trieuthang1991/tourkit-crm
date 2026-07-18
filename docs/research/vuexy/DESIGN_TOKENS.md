# Vuexy Design Tokens — extracted from live demo

Source: https://demos.pixinvent.com/vuexy-html-admin-template/html/vertical-menu-template/
Extracted via getComputedStyle / :root CSS variables. All values verbatim.

## Palette (semantic)
| Token | Value | Note |
|-------|-------|------|
| primary | `#7367f0` | Vuexy purple |
| secondary | `#808390` | grey |
| success | `#28c76f` | green |
| info | `#00bad1` | cyan |
| warning | `#ff9f43` | orange |
| danger | `#ff4c51` | red |

## Surfaces & text
| Token | Value |
|-------|-------|
| body-bg (canvas) | `#f8f7fa` |
| card / surface | `#ffffff` |
| body-color (text) | `#6d6b77` |
| heading-color | `#444050` |
| border-color | `#e6e6e8` |
| menu-active-bg | `~#eeedf0` (srgb 0.9347 0.9335 0.9391) |

## Typography
- Font family: **"Public Sans"**, -apple-system, "Segoe UI", ... (Google Font)
- Body font-size: `0.9375rem` (15px)
- Body color: `#6d6b77`
- Card title: 18px / weight 500 / #444050 / lh 28px
- h4: 24px / 500 / lh 38px
- Table th: 13px / 500 / **UPPERCASE** / letter-spacing 0.2px / #444050

## Shape & elevation (SIGNATURE)
- Card: radius **6px**, no border, shadow `rgba(47, 43, 61, 0.14) 0 3px 12px 0`
- Button: radius **4px**, primary glow shadow `rgba(115, 103, 240, 0.3) 0 2px 6px 0`
- Sidebar / navbar shadow: `rgba(47, 43, 61, 0.12) 0 2px 8px 0`
- **Shadows are tinted `#2f2b3d` (dark purple-grey), NOT pure black** — key Vuexy tell.

## Shell
- **Sidebar**: 260px wide, white bg, soft shadow, no right border.
  - menu-link: 15px / 400 / #444050, padding 8px 12px, radius 6px
  - active item: filled **purple** pill (#7367f0) white text + subtle glow (submenu active = light grey pill)
  - section headers ("APPS & PAGES") = tiny grey caps
- **Navbar**: floating, height 56px, white, radius 6px, `backdrop-filter: saturate(2) blur(6px)`, shadow.
  - full-width search "Search [CTRL+K]"; right: i18n, theme toggle, apps grid, bell(dot), avatar(online dot)
- **Content**: bg #f8f7fa, cards white 6px, generous gaps (~24px)
- **Footer**: "© made with ♥ by Pixinvent" left; License / More Themes / Documentation / Support right

## Component signatures
- Stat card: small title + subtitle (grey) → big number → colored % delta chip (soft green/red)
- Soft badges: light tinted bg + colored text (Verified=green, Rejected=red, Pending=grey)
- Menu badge counts (red pill), submenu ○ bullets, expand chevrons
