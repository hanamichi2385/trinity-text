# Domain model and persistence

Entities are `partial`, `virtual`, UPPERCASE properties, implement `IEntity`. DB column/table names are legacy Italian (Vendor = Website, Istanza/Pricelist = Site).

- **Tenancy**: Website and Site are **plain strings** (`FK_WEBSITE`→col `FK_VENDOR`, `FK_PRICELIST`→`FK_ISTANZA`), not entities. Null/empty website = global row. No global query filter: every service query applies scope itself; search DTOs carry `UserWebsites[]`/`WebsiteLanguages[]` that callers must supply.
- `Text` (`Risorse`) 1‑N `TextRevision` (`TestiPerRisorsa`); `Text` N‑1 `TextType` (`TipologieTesti`); `TextTypePerWebsite` join.
- `Page` (`Contenuti`, XML in CONTENT) N‑1 `PageType` (`TipologieContenuti`, XML schema, `Subfolder`, `OutputFilename`, VISIBILITY = `|`-joined).
- `Widget` (`WidgetContenuti`, KEY + CONTENT).
- `Folder` (`Cartelle`, self-ref `FK_PARENT`, `DELETABLE`) / `File` (`Files`, GUID key, CONTENT + THUMBNAIL blobs). FK ids only, no navigations.
- `Publication` (`Generazioni`; PAYLOAD = JSON `PayloadDTO`; ZIP_FILE column is **not** on the entity) → optional `CdnServer`, `FtpServer`. Join tables: `CdnServersPerWebsite`, `FtpServerPerCdnServer`. `CacheSettings` (per CDN, JSON payload), `WebsiteConfiguration`.

## Persistence specifics
- **EF**: fluent config inline in `TrinityEFContext.OnModelCreating`; context pooled, `NoTracking`, `AutoDetectChanges=false`. AutoInclude on `Page.PAGETYPE`, `Text.TEXTTYPE`, `Publication.CDNSERVER/FTPSERVER`, CDN→FTP. `Text.REVISIONS` is deliberately **not** auto-included (load latest via `TextService.PopulateLatestRevisions`).
- **EF `Update`** = `ChangeTracker.Clear()` + `Update(entity)` (whole graph) and each op calls `SaveChanges` immediately → populate entities fully before updating.
- **NH**: dynamic `ClassMapping`s, schema `dbo`, lazy blobs, FK scalars mapped `Insert(false)/Update(false)`; `Create`/`Delete` don't flush, commit does.
- No unit-of-work class: services call `BeginTransaction/CommitTransaction/RollbackTransaction` on the repository.
- `IRepository<T>`: `Create/Read/Update/Delete/AddRangeAsync`, `ToListAsync/FirstOrDefaultAsync/CountAsync(IQueryable)`, `ExecuteDeleteAsync`, `Repository` (IQueryable), `ConnectionString`, transactions. IQueryable helpers are provider-neutral (queries from other repositories can be passed in).
