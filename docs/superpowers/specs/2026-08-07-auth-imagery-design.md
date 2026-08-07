# Auth Imagery Design

## Goal

Add professional, brand-aligned imagery to the React `/login` and `/register` screens without distracting from form completion or changing authentication behavior.

## Visual Direction

Use a hybrid travel-and-data art direction. Each asset combines a cinematic Vietnamese travel landscape with restrained route lines and translucent, text-free operational cards. The treatment must feel editorial and premium rather than like generic 3D SaaS artwork.

- **Login:** cool violet and cyan scene suggesting an active, well-controlled tour operation.
- **Register:** warmer violet and amber sunrise suggesting a new business journey.
- **Shared constraints:** portrait 2:3 composition, subject concentrated toward the lower-right, generous negative space for copy, no people as focal subjects, no embedded text, logos, trademarks, or watermark.

## Integration

Store optimized assets under `web/src/assets/auth/`. Render the relevant image inside the existing `mk-auth__brand` panel, behind the current logo, heading, feature list, and footer. Use a gradient overlay and low visual intensity to preserve white-text contrast. Keep the existing mobile behavior: the entire brand panel, including imagery, remains hidden below 900 px.

Login and registration receive separate images but share one reusable CSS treatment. Forms, validation, API calls, routes, and success states remain unchanged.

## Quality Checks

- Confirm both images match the TourKit palette (`#7367f0`, cyan accents, restrained amber).
- Confirm no generated text or watermark appears.
- Verify desktop readability at common viewport heights and no image overlap with interactive controls.
- Run frontend lint, tests, and production build after integration.
