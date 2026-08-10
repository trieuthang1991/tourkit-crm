# M-Travel Pricing Design

## Goal

Replace the current tiered monthly/annual pricing with a simple employee-based model that is immediately understandable on the landing page.

## Pricing Rules

- Companies with up to and including 20 employees use M-Travel free of charge.
- Companies with more than 20 employees pay only for employees above the free allowance, at 60,000 VND per employee per month.
- Example: a company with 30 employees pays `(30 - 20) × 60,000 = 600,000 VND/month`.
- Companies requiring custom workflows, integrations, security, or deployment receive a tailored quote.

## Presentation

Keep the current three-card pricing layout and visual hierarchy:

1. **Miễn phí** — emphasizes the 20-employee allowance.
2. **Theo quy mô** — the highlighted card, showing `60.000đ/người/tháng` for the portion above 20 employees and the 30-employee example.
3. **Theo yêu cầu** — directs larger or specialized deployments to consultation.

Remove the monthly/annual selector because the new pricing has only a monthly rate. Update the comparison-table headings and rows to match the same three choices. Preserve existing reveal and card-hover animations.

## Verification

The Razor Page smoke test must assert the free threshold, overage rate, calculation example, custom option, and absence of the obsolete annual-pricing control. Browser verification must confirm all three cards render correctly on desktop and mobile without console errors.
