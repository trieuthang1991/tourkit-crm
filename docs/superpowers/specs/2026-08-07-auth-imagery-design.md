# M-Travel SaaS Brand and Auth Visual Design

## Goal

Replace visible TourKit branding with **M-Travel** and give the active Razor Pages authentication screens a distinct SaaS-management visual identity. Internal `TourKit.*` namespaces, assemblies, database names, and APIs remain unchanged.

## Identity System

Use a **Modular M** mark: a compact rounded-square symbol whose M is assembled from connected dashboard modules. The geometry suggests coordination, structured data, and forward movement without using aircraft, pins, landscapes, or other consumer-travel clichés.

- Primary violet: `#7367F0`
- Deep ink: `#2F2B3D`
- Signal cyan: `#00BAD1`
- Wordmark: `M-Travel`, using the application's Public Sans typography

Create a deterministic SVG mark for navigation, auth pages, and favicon, plus a 180px PNG for Apple touch icons. `_BrandMark.cshtml` remains the single reusable source for the visible mark and wordmark.

## Auth Artwork

Generate two portrait 2:3 bitmap backgrounds with no readable text, logos, people, or tourism scenery:

- **Login:** a premium dark SaaS operations workspace made from layered CRM pipeline, task board, calendar, and analytics modules connected by subtle data paths.
- **Register:** modular interface blocks assembling into one coordinated operating system, suggesting setup and activation.

Both images use violet/ink/cyan, controlled glass depth, asymmetric composition, and generous left-side negative space for HTML copy. They remain desktop-only through the existing Bootstrap `lg` breakpoint.

## Brand Coverage

Update the shared mark, browser title, metadata, favicon links, layout variables, footer, auth headings/copy, and the company short-name placeholder. Keep changes confined to user-visible branding; do not rename technical symbols or URLs.

## Verification

- Razor smoke tests require distinct auth artwork and visible `M-Travel` branding.
- Search user-facing Razor/layout files for stale `TourKit` copy while excluding namespaces.
- Verify SVG/PNG/WebP dimensions and absence of generated text/watermarks.
- Run focused tests, Release build, diff checks, and desktop/mobile visual QA.
