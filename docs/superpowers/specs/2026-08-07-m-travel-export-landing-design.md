# M-Travel Export Landing Integration Design

## Objective

Replace the current public Razor landing page with the approved design exported in `D:\MiGroup\AI\tourkit-crm\export`, while preserving M-Travel branding, anonymous access, authentication routes, responsive behavior, and accessibility.

## Source of Truth

The export's `index.html`, `styles.css`, `tailwind.config.js`, and `script.js` define the approved visual design and content. The implementation will preserve its sections, copy, interactions, pricing, customer names, hotline, and AI messaging. Existing M-Travel logo assets will replace the export's letter-only logo.

## Integration

- Convert `index.html` into `src/TourKit.Api/Pages/Index.cshtml` with `@page`, `IndexModel`, Razor asset helpers, canonical metadata, and favicon links.
- Map login links to `/dang-nhap`, trial CTAs to `/dang-ky`, and demo CTAs to the export's contact/CTA section.
- Compile the exported Tailwind utilities into a self-hosted production stylesheet. Do not load Tailwind Play CDN or its runtime configuration in the browser.
- Merge the export's supplemental CSS into the landing stylesheet and keep Be Vietnam Pro as the display/body family.
- Port the export JavaScript to the existing landing script, preserving mobile navigation, role tabs, pricing toggle, FAQ, ticker, and reveal behavior.
- Keep all landing assets under `src/TourKit.Api/wwwroot`; do not change authenticated application styling.

## Behavior and Accessibility

The page remains available anonymously at `/`. Navigation anchors, keyboard focus, skip navigation, semantic landmarks, image alternatives, and `prefers-reduced-motion` must work. JavaScript-enhanced content must have readable initial or fallback states.

## Verification

- Update the landing smoke test to assert the export's identifying content, local CSS/JS, authentication routes, and absence of Tailwind CDN.
- Run focused red/green verification, JavaScript syntax checking, a Release build, and the full test suite.
- Inspect desktop, tablet, and mobile layouts; confirm no horizontal overflow, console errors, missing assets, or broken CTA destinations.

## Scope

Only the public landing page and its dedicated assets/tests are changed. Login, registration, authenticated Razor pages, APIs, and business workflows remain untouched.
