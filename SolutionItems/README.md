#2026-10-06
- Tests: `SourceConventionTests` (offline) fail on runtime `IOrderedQueryable` checks, `Task.FromResult(...ToList())` and EF/NH `using` in `Core/TrinityText.Business`
- CI: pull requests that change `Core/**` without a `SolutionItems/README.md` change fail the build (`builds.yml`, "Check changelog entry")

#2026-10-01
- Security: AutoMapper 14 (GHSA-rvv3-g6hj-g44x, DoS via uncontrolled recursion; fixes only in the commercial 15.x/16.x) replaced by Riok.Mapperly 4.3 (Apache-2.0, source generator: no reflection, no runtime graph walking). `BusinessMapperProfile` -> internal `BusinessMapper`; services no longer take `IMapper` (hosts must remove `AddAutoMapper(BusinessMapperProfile)`). Unmapped DTO/entity members now break the build (RMG012)
- Refactor: version 1.4.0 (public API: services lose the `IMapper` constructor parameter); mapping behavior is unchanged
- Tests: `BusinessMapperTests` for every custom mapping rule

#2026-09-30
- License: rollback to Automapper 14.0.0

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
- CI: `releases.yml` runs only on `published` (before: `published/edited/released` produced several packages per release), least-privilege `permissions`, `concurrency`, timeouts, API key via `env`, `environment: nuget`, offline tests before publishing, single build + `pack --no-build`; `builds.yml` also on pull requests, runs the offline tests and reports vulnerable packages; new `dependabot.yml` and `codeql.yml`
- Packaging: symbol packages (`snupkg`), SourceLink, deterministic CI builds, MIT license acceptance not required; `SkiaSharp.NativeAssets.Linux.NoDependencies` added so image processing works on Linux
- Fix: publish failure keeps the publication and its ZIP (status `Failed`) instead of deleting them; `Generate`/`Publish` consumers are idempotent (completed publications are skipped, a `Publishing` one only re-sends the publish message); mails only when an address is set and never with line breaks in the subject
- Fix: a missing/corrupt ZIP or a failed compression is an error (before: empty upload reported as success, empty path returned); partial ZIP and export folder are removed on failure
- Fix: export failures (folders/files/content not readable) abort the publication instead of producing an incomplete one; export paths use `Path.Combine` (Linux-safe) and are confined to the export root; site / language codes of the payload are validated; invalid XML characters removed from exported texts; empty `OutputFilename` falls back to the page type name; page errors report the page id and title
- Fix: `CleanRevisions` rejects a limit lower than 1 (deleted every revision) and counts by `REVISION_NUMBER`
- Fix: text selection no longer throws (`Single`) or silently drops texts when the same key exists at several levels
- Fix: null and empty website/site/country are equivalent in duplicate checks and publish queries; text names upper-cased with the invariant culture (also on update); updating a text adds a revision even when none was loaded and never reuses the id of an existing revision; `TextDTO` -> `Text` no longer maps the nested `TextType`
- Fix: `RemoveFolder` deletes files and subfolders (foreign keys do not cascade); `SaveFolder` refuses to edit system folders, keeps the website and rejects duplicate sibling names; `AddFile` requires the folder to belong to the website; file links are unescaped when resolved; `GetAllFolders` sets `Deletable`; `PasteFile`/`MoveFile` error codes were swapped; moving a file into its own folder is a no-op
- Fix: optional Number/DateTime page atoms can be empty; invariant culture in the page schema
- Fix: `Begin/Commit/RollbackTransaction` nest (EF: an inner Begin used to roll back the outer transaction); CDN update is transactional and uses targeted UPDATEs; widget update checks duplicates; `PublicationService.Create` returns FTP/CDN, `Get` accepts a publication without payload
- Fix: SFTP `GetFile` no longer returns a null Task, clients are disposed, files opened read-only, port honored; remote directory taken from the URL path (no more `user:password@` / port as folder names); AWS translator reuses one client and reports errors instead of returning an empty translation; Excel import fails on unreadable files, collapses repeated rows, blank scope cells are null; widget lookup errors are no longer published as the widget key; thumbnail options validated
- Tests: `[TestCategory("Offline")]` tests (no database) for PathSafety, WidgetUtilities, text selection, atoms, mapping, ZIP, transfer and the publish/generate consumers (MassTransit test harness)
- Fix (NHibernate): `Create` returned the identifier cast to the entity (InvalidCastException) and `Read` passed the id array to `GetAsync`; `Create/Update/Delete` were never flushed outside a transaction; the mapping lacked `FtpServer.PASSWORD`, `Publication.EMAIL/PAYLOAD/FK_FTPSERVER/FK_CDNSERVER`, `WebsiteConfiguration.URL/NOTE`; `Text.TEXTTYPE` was mandatory; new revisions and CDN join rows were not persisted (cascade); many-to-one `DeleteOrphans` could delete the referenced entity; foreign keys to existing rows are now written from the scalar column (like EF Core) and the join tables use scalar composite keys
- Fix: `ExecuteDeleteAsync/ExecuteUpdateAsync` ignore auto-includes (EF Core threw `ArgumentOutOfRangeException` on `FtpServerPerCdnServer`, breaking the CDN update); untyped texts were never published with NHibernate (`IN (.., NULL)`); duplicate check and import compare upper-cased names; CDN creation inserts the join rows after the server
- Security: `PathSafety` also rejects control characters, Windows device names (CON, NUL, COM1, ...), trailing dots and names longer than 255
- Security: widget token regex bounded (no quadratic scan on unclosed `@[WIDGET(`) with a timeout, input size limit; nested widgets are expanded first and the `]]>` protection is applied once to the whole expansion (a widget ending with `]` nesting one starting with `]>` could close the CDATA)
- Security: `FileUploadPolicy` blocks server-side scripts, executables and web server configuration files (`AddFile`, `RenameFile`); CDN base URL must be http(s); FTP/SFTP address must be `ftp://` / `sftp://` without credentials; the transfer accepts only those schemes
- Security: Excel import refuses rows of websites the caller cannot manage (`allowedWebsites` on `IExcelService.GetTextsFromStream` and `ITextService.ImportTexts`), invalid site/language names and sheets over 50,000 rows
- Security: pages must be well-formed XML (no DTD, at most 200 levels, 5 M characters) to be saved; widgets are limited to 1 M characters; `SafeXml` limits characters and depth (the JSON conversion is recursive); `GetPage` caps the page size (1000) and cannot overflow
- Security: vulnerable transitive packages pinned (`System.Text.Json` 8.0.5, `System.Security.Cryptography.Xml` 10.0.12, `System.Net.Http` / `X509Certificates` for NHibernate); the CI check of vulnerable packages now fails the build
- Tests: SQLite in-memory fixture running the repository and service tests on EF Core and NHibernate, EF/NH/script parity tests, hardening tests
- Performance: `TextType.TEXTTYPEPERWEBSITES` is no longer auto-included (every text row was multiplied by the number of websites of its type)
- Performance: latest-revision loading and text import query in chunks of 1000 ids/names (no huge `IN (...)` lists); re-importing an unchanged sheet with override does not rewrite the texts
- Performance: `PageService` search / publishable pages read the page columns only and load each `PageType` (with its schema XML) once; no SQL ORDER BY on the export; `Save` uses a targeted UPDATE and `Remove` a set-based delete
- Fix: publishable pages of a site no longer include pages of languages the site does not publish
- Performance: `GetFile` reads only the requested blob (content or thumbnail); overriding a file updates it in place without reading the old blobs; `Remove` of widgets / publications is a set-based delete; `GetAllRevisions` is ordered and does not load the text
- Performance: one widget / link cache and one parsed schema per page type for the whole export; texts grouped once per language; ordinal placeholder replacement; `GalleryAtom.Validate` is linear
- Performance: publication ZIP streamed from the database to a temporary file (`IPublicationService.CopyZipTo`) and extracted off the consumer thread (`ICompressionFileService.DecompressFile`); `PublishWebsiteConsumer` no longer loads the ZIP in memory; ZIP built with `CompressionLevel.Fastest`
- Performance: `SFTPTransferService` uses the asynchronous SSH.NET API (no thread blocked for the whole transfer)
- Performance: NHibernate batch fetching on `TextType`, `PageType`, `FtpServer`, `CdnServer`, `Folder`
- New: `Db/indexes.sql` (foreign key and widget indexes) and `Db/optional_optimizations.sql` (`Contenuti.CONTENUTO` ntext -> nvarchar(max), unique index on text revisions): run manually
- Tests: page / widget behavior on both providers, end-to-end export and publish with fake services

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