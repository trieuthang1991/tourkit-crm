# Memory Index

- [Project layout & tooling](project-layout-and-tooling.md) — real path is nested `tourkit-crm/tourkit-crm`; GitNexus + Memory Compiler wiring
- [No tables for tooling data](no-tables-for-tooling-data.md) — keep tool data file-based, never add DB tables/schema for it
- [Business logic follows old project](business-logic-follow-old-project.md) — reference legacy system (script.sql / old repo) for tour business logic, don't invent
- [Roadmap status](roadmap-status.md) — Đợt 0-6 done (PR #2); what's left and which items need owner decisions
- [External providers → API Gateway](external-providers-gateway.md) — SMS/Zalo/Bank/OCR/CRM go through ONE central API Gateway, not per-provider clients; vendors chosen (eSMS/ZNS/Casso/FPT.AI), keys pending
- [Legacy UI reference](legacy-ui-reference.md) — staging.tourkit.vn live UI to match; brand #EB5324 + Roboto + #333 sidebar; workspace landing layout
- [Entity extend: JSON + string-ID pattern](entity-extend-json-string-pattern.md) — extending models to match legacy: JSON column for soft/list fields, STRING ids (not Guid) for migration, multi-value lists
- [Screens need search + stats](screen-needs-search-and-stats.md) — every list screen needs a stats-card row + search/filter toolbar on top (follow legacy), not just the table
