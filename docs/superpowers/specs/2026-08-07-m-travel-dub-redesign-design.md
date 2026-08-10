# M-Travel Dub-Style Landing Redesign

## Design Read

Redesign-overhaul for a B2B SaaS landing page aimed at owners and operations teams. The visual language follows the supplied Dub reference: editorial, compact, product-first, light-only, and defined by hairline borders instead of effects.

Design dials: `DESIGN_VARIANCE 5`, `MOTION_INTENSITY 3`, `VISUAL_DENSITY 4`.

## Goals

- Make M-Travel look like a serious management product rather than a technology demo.
- Put the real dashboard screenshot above the fold as the primary visual.
- Preserve `/`, `/dang-nhap`, `/dang-ky`, current anchor IDs, SEO metadata, logo, and Vietnamese copy intent.
- Remove 3D, violet glow, oversized bold type, automatic dark mode, and decorative illustration treatment.

## Visual System

- Canvas: `#ffffff`; alternate paper: `#f5f5f5`; border: `#e5e5e5`.
- Text: `#171717`; muted: `#737373`; single primary accent: `#2563eb`.
- Primary CTA: `#0a0a0a` with white text. Blue is reserved for links, active details, and small accents.
- Display type: Satoshi if locally available, otherwise Inter/Arial with weight 500, 48px maximum, and tight line height. Body/UI uses Inter-style system sans at 14-16px.
- Shape vocabulary: pills 9999px, buttons 8px, cards 12px, large product surfaces 16px.
- Containers use 1px borders. Shadows appear only as a subtle ring around the hero dashboard.

## Page Composition

1. A non-sticky 68px navigation with logo, four anchors, outlined login, and one dark signup CTA.
2. A centered hero containing one eyebrow, a two-line headline, a sub-20-word value statement, two CTAs, and three compact feature pills.
3. The real M-Travel dashboard screenshot immediately below the hero copy in a product mockup frame.
4. A compact operating-flow rail using hairline separators.
5. An asymmetric five-cell capability grid. The existing SaaS illustration is retired from this page; small product details and icons carry the visual rhythm.
6. A product-proof section using a cropped detail of the same real dashboard and concise operational copy.
7. A three-part adoption narrative rendered as a single bordered strip rather than equal floating cards.
8. A light paper CTA and minimal footer.

## Interaction and Accessibility

- Motion is limited to one-time IntersectionObserver reveals and hover/active feedback using only opacity and transform.
- `prefers-reduced-motion` renders all content immediately.
- No Three.js, canvas, continuous animation, scroll listener, stock photography, fake UI, or section-level theme inversion.
- Keyboard focus remains visible. Semantic landmarks, alt text, and CTA contrast are preserved.
- Desktop hero fits the first viewport. Mobile collapses to one column with no horizontal overflow and the dashboard remains directly after the feature pills.

## Verification

- Razor smoke test proves `/` is anonymous, product-first, and contains no Three.js/canvas dependency.
- Release build and all solution tests pass.
- Browser QA covers 1440px, 393px, 320px, reduced motion, console errors, overflow, and asset loading.

