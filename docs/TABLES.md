# Table handling strategy
AOS must never assume that visual table preservation is the most accessible output.

Planned modes:
1. Preserve structure: export real Word/HTML tables with logical row/column order.
2. Accessible linearization: emit table title, row number, header/value pairs for nonvisual reading.
3. Text only: preserve cell text without claiming table semantics.

Detection confidence must be exposed when available. Ambiguous layouts should be reviewable rather than silently reconstructed. Arabic tables must preserve RTL cell text while maintaining logical header relationships.
