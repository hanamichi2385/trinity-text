#2026-09-29
- Security: `TextType.Name` validated with `PathSafety` (used as export file name); export file paths canonicalised and confined to the export root (`PathSafety.EnsureWithinRoot`)
- Security: widget expansion capped at 5 MB total (nested widgets could grow exponentially)
- Security: tenant / website / file names validated in `TransferService` before FTP/SFTP navigation; publication payload website must match the publication website (Generate/Publish)
- Security: removed hard-coded `[TRINITY]` database name from raw SQL in `PublicationService`; `UpdateZipContent` failures are now propagated
- Security: `IPublicationService.GetAll` accepts an optional `websites` filter (null keeps previous behavior)
- Security: widget content and placeholder values are CDATA-safe (`]]>` can no longer close the section and inject markup into published pages)
- Security: XML schemas/contents parsed with DTD processing prohibited (`SafeXml`)
- Security: file names validated with `PathSafety`; system folder names (site / language) validated in `CreateFolderByName`
- Security: `SaveFolder` rejects missing / cross-website parents and cycles; `GetFileLink` is loop-safe
- Security: images declaring more than `MaxPixels` (default 50 MP) are not decoded; null codec handled
- Security: HTML-encoded values in publish/generate notification mails; null-safe FTP server name
- Performance: `GetFileIdByFullname` (id only, no `CONTENT`) used for link resolution; new `WidgetResolutionCache` memoizes widget and link lookups across the pages of an export (new overloads on `IWidgetUtilities`)
- Performance: `DeleteFile` via set-based delete and `GetFileLink` via metadata projection (no blob loading)
- New: `ExecuteUpdateAsync` on `IRepository<T>` (set-based UPDATE of selected columns; EF Core + NHibernate implementations, `UpdateSetters<T>`)
- Performance: `RenameFile` and `MoveFile` update the row in place (no blob load/rewrite); a moved file keeps its id (before: new row + delete)
- Performance: file export loads one blob at a time (`IFileManagerService.GetFileContent`) instead of the whole folder; export paths confined to the export root
- Performance: publication ZIP streamed from disk to SQL (`IPublicationService.UpdateWithZipStream`, VarBinary(max)); publication status updated with a targeted UPDATE (no entity graph rewrite)
- Performance: publishable pages loaded without repeating the PageType schema XML on every row (page types loaded once)
- Performance: `ImportTexts` inserts new texts in a single batch (`AddRangeAsync`); Excel import resolves text types via dictionary
- Performance: multi-sheet Excel export in memory (no temp file)
- Performance: remaining synchronous reads (`GetAll*`, `NotDuplicated`, folder/file lookups) are now async; `PublicationService.GetAll` no longer reads FTP credentials (new `IRepository<FtpServer>` constructor dependency, resolved by DI)
- Performance: FTP transfers use FluentFTP `AsyncFtpClient`
- Performance: thumbnails decoded directly at reduced size (`SKCodec.GetScaledDimensions`) with linear sampling; one codec per image operation (size limit, animation check and decode share it)
- Performance: `GetPublishableTextsByWebsite` groups global texts by language once (`ToLookup`, `Concat` instead of `Union`); `GetPublishableTexts` skips the redundant SQL ORDER BY
- Performance: `CleanRevisions` is a single set-based DELETE (no id list materialised)
- Performance: default site / language folder creation reads the parent's children once
- Performance: `WidgetService.GetByKeys` is async; owner/website filters use `col == null || col == ""` instead of `IsNullOrWhiteSpace` (index friendly)
- New: `Core/TrinityText.Domain/Db/indexes.sql` with recommended non-clustered indexes (run manually on the database)

#2026-06-12
- Performance: `Text.REVISIONS` no longer auto-included; only the latest revision is loaded per text via a single correlated query (was eager-loading every historical revision with full content on every read)
- Performance: removed redundant circular `TextRevision.TEXT` auto-include
- Performance: `CleanRevisions` deletes excess revisions via SQL correlated count (no revision content loaded into memory)
- Security: `GetFileByFullname` resolves same-named files by folder path (was `NotImplementedException` on multi-match)
- Security: `RemoveFolder` refuses non-deletable folders and folders whose subtree contains system folders
- Security: path-segment validation (`PathSafety`) on folder names, TextType/PageType `Subfolder` and `OutputFilename` to prevent traversal
- Fix: `Search` sorting crash when only some sort fields were set (`Nullable.Value` on unset field)
- Fix: `TransferService.GetFile` now returns the downloaded bytes (were discarded)
- Fix: `TrinityEFContext` transaction field reset on commit/rollback + guarded begin (stale transaction from pooled context)

#2026-06-11
- New: `CountAsync` on `IRepository<T>` (EF + NHibernate implementations)
- Performance: async `Search` in TextService / PageService / WidgetService (`CountAsync` + `ToListAsync`)
- Performance: async `WriteAllBytesAsync` / `ReadAllBytesAsync` in publication generation pipeline
- Performance: O(n²) → O(n) folder tree build via `ToLookup(ParentId)`
- Performance: direct DTO projection in `GetAllFolders` (no entity intermediate, async)
- Performance: `MaxBy` instead of `OrderByDescending().FirstOrDefault()` in TextRevision mapping
- Performance: `JRaw` in `CreateJsonContentsDocument` (no double-encoding, no dynamic dispatch)
- Performance: `ExcelMapperService` single-sheet exports via `MemoryStream` (no temp file)
- Performance: retry with exponential backoff in `GenerateWebsiteConsumer` (was busy retry, no delay)
- Refactor: `Sort` extension — explicit `Unordered` handling, safer casts; preserves `IQueryable.Expression.Type` check (EF Core concrete queryable implements `IOrderedQueryable<T>` even before sorting, so runtime-type checks are unsafe)
- Fix: `SerializedFile` async factory (`Path.GetFileName`, `ReadAllBytesAsync`)

#2026-06-10
- New: provider-agnostic `IRepository<T>` (EF Core + NHibernate) with async query and bulk operations
- Performance: eliminate N+1 queries in publishing, import, and website configuration flows
- Performance: bulk `ExecuteDeleteAsync` for revisions and join tables
- Performance: O(n²) → O(n) in text reduction via `ToLookup` indexing
- Performance: direct DTO projection in file listing (optional blob columns)
- Performance: widget resolution with compiled regex and per-call batched lookups
- Performance: XML parsing via `XDocument.Parse`, element lookup via `ToLookup`
- Performance: async blob streaming in publications (`GetFieldValueAsync`, sequential access)
- Performance: stream-based FTP/SFTP transfers, deduplicated file enumeration
- Fix: duplicate FTP entries on CDN update
- Fix: SkiaSharp native resources properly disposed; correct output buffer

#2026-06-03
- Security: add filename and folder name validation in FileManagerService (trim, invalid chars check)
- Security: fix duplicate assignment of NAME in SaveFolder
- Performance: fix N+1 query in GetFileLink (single query + in-memory traversal)
- Performance: fix N+1 query in CheckFileToFolder (load filenames once into HashSet)
- Performance: fix loop DELETE on text revisions in TextService.Remove
- Performance: remove unused recursive method GetAllSubfoldersByFolder
- Performance: remove async/await Task.FromResult overhead across all service methods (26 occurrences)
- Refactor: fix multi-dot filename handling in CheckFileToFolder (use LastIndexOf)

#2026-03-16
- Fix SFTPTransferService
- Optimize ZipCompressionService

#2025-02-13
- Add useOriginalFormat to AddFile interface
- Introduce useMobile attribute in atom

#2025-02-05
- Add SeparatorAtom support
- Add "Extend" property in TextAtom
- Fix "Mobile", "Target" support in ImageAtom
- Add extra name, description support in ImageParticol 

#2025-01-27
- Fix publish error email
- Update nuget packages

#2024-11-04
- Fix gif animate compression bug

#2024-10-30
- Improve export algorithm

#2024-10-29
- Remove formatting problems

#2024-10-28
- Clean code for better performance
- Update packages

#2024-04-04
- Update packages

#2024-03-31
- Update packages
- Fix warning

#2024-01-31
- Migrate to NET8

#2023-10-05
- Add DateTime Atom

#2023-07-08
- Migrate to NET6