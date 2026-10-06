# Gotchas

Checked against the code on 2026-10-06: removed the entries fixed by audit round 2 (hard-coded `[TRINITY]` DB name / swallowed `UpdateZipContent` errors, `DecompressFolder` swallowing errors, `\\` separators, `Task.FromResult(.ToList())`). Entries marked (unchecked) were not re-verified.

- Excel export columns differ between exports (TEXT vs CONTENT, COUNTRY missing in multi-sheet); importer reads TEXT/COUNTRY (unchecked).
- `WidgetService` `ShowOnlyDedicated` filters non-empty website regardless of the boolean (`TextService` honours the value).
- `GetFileByFullname` loads CONTENT for every filename match (unchecked; see S1 in `CLAUDE.md`).
- Three entities override `Equals/GetHashCode` with `base` + TODO (reference equality on NH composite ids) (unchecked).
