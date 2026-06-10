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