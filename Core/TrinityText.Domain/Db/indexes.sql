-- Recommended non-clustered indexes for the Trinity schema (SQL Server).
-- create_table.sql only defines clustered primary keys; the filters below are used by the services
-- (search, publish, duplicate checks, revision handling, file manager).
-- Idempotent: safe to run more than once. Review on a copy of production first.
-- Note: '[FK_VENDOR] IS NULL OR = ''''' style filters (global rows) are written as `col == null || col == ""` in the
-- services so that these indexes can be used.

-- Revisions: latest revision per text (correlated MAX) and CleanRevisions (correlated COUNT)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TestiPerRisorsa_Risorsa_Revisione' AND object_id = OBJECT_ID('[dbo].[TestiPerRisorsa]'))
    CREATE NONCLUSTERED INDEX [IX_TestiPerRisorsa_Risorsa_Revisione]
    ON [dbo].[TestiPerRisorsa] ([RISORSA], [REVISIONE]);

-- Texts: search / publish by website and language
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Risorse_Vendor_Lingua' AND object_id = OBJECT_ID('[dbo].[Risorse]'))
    CREATE NONCLUSTERED INDEX [IX_Risorse_Vendor_Lingua]
    ON [dbo].[Risorse] ([FK_VENDOR], [FK_LINGUA])
    INCLUDE ([FK_ISTANZA], [FK_TIPOLOGIA], [ATTIVA]);

-- Texts: duplicate check and import lookup by name
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Risorse_Nome_Lingua' AND object_id = OBJECT_ID('[dbo].[Risorse]'))
    CREATE NONCLUSTERED INDEX [IX_Risorse_Nome_Lingua]
    ON [dbo].[Risorse] ([NOME], [FK_LINGUA]);

-- Pages
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contenuti_Vendor_Lingua_Attiva' AND object_id = OBJECT_ID('[dbo].[Contenuti]'))
    CREATE NONCLUSTERED INDEX [IX_Contenuti_Vendor_Lingua_Attiva]
    ON [dbo].[Contenuti] ([FK_VENDOR], [FK_LINGUA], [ATTIVA])
    INCLUDE ([FK_ISTANZA], [FK_TIPOLOGIA]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contenuti_Tipologia' AND object_id = OBJECT_ID('[dbo].[Contenuti]'))
    CREATE NONCLUSTERED INDEX [IX_Contenuti_Tipologia]
    ON [dbo].[Contenuti] ([FK_TIPOLOGIA]);

-- Widgets: GetByKeys (one lookup per widget key while publishing)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WidgetContenuti_Chiave_Lingua' AND object_id = OBJECT_ID('[dbo].[WidgetContenuti]'))
    CREATE NONCLUSTERED INDEX [IX_WidgetContenuti_Chiave_Lingua]
    ON [dbo].[WidgetContenuti] ([CHIAVE], [FK_LINGUA])
    INCLUDE ([FK_VENDOR], [FK_ISTANZA]);

-- Files: name check inside a folder, and resolution of @/website/... links by file name
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_Folder_Filename' AND object_id = OBJECT_ID('[dbo].[Files]'))
    CREATE NONCLUSTERED INDEX [IX_Files_Folder_Filename]
    ON [dbo].[Files] ([FK_FOLDER], [FILENAME]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_Filename' AND object_id = OBJECT_ID('[dbo].[Files]'))
    CREATE NONCLUSTERED INDEX [IX_Files_Filename]
    ON [dbo].[Files] ([FILENAME])
    INCLUDE ([FK_FOLDER], [FK_VENDOR]);

-- Files: incremental export (files modified since a date)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_Vendor_Folder_Modifica' AND object_id = OBJECT_ID('[dbo].[Files]'))
    CREATE NONCLUSTERED INDEX [IX_Files_Vendor_Folder_Modifica]
    ON [dbo].[Files] ([FK_VENDOR], [FK_FOLDER], [DATA_ULTIMA_MODIFICA]);

-- Folders: tree by website / parent
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cartelle_Vendor_Parent' AND object_id = OBJECT_ID('[dbo].[Cartelle]'))
    CREATE NONCLUSTERED INDEX [IX_Cartelle_Vendor_Parent]
    ON [dbo].[Cartelle] ([FK_VENDOR], [PARENT_FOLDER]);

-- Publications: listing ordered by last update, per website
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Generazioni_UltimoAggiornamento' AND object_id = OBJECT_ID('[dbo].[Generazioni]'))
    CREATE NONCLUSTERED INDEX [IX_Generazioni_UltimoAggiornamento]
    ON [dbo].[Generazioni] ([ULTIMO_AGGIORNAMENTO] DESC)
    INCLUDE ([FK_VENDOR]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Generazioni_CdnServer' AND object_id = OBJECT_ID('[dbo].[Generazioni]'))
    CREATE NONCLUSTERED INDEX [IX_Generazioni_CdnServer]
    ON [dbo].[Generazioni] ([FK_CDNSERVER]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Generazioni_FtpServer' AND object_id = OBJECT_ID('[dbo].[Generazioni]'))
    CREATE NONCLUSTERED INDEX [IX_Generazioni_FtpServer]
    ON [dbo].[Generazioni] ([FK_FTPSERVER]);

-- Join tables / FKs used by deletes and lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CacheSettings_CdnServer' AND object_id = OBJECT_ID('[dbo].[CacheSettings]'))
    CREATE NONCLUSTERED INDEX [IX_CacheSettings_CdnServer]
    ON [dbo].[CacheSettings] ([FK_CDNSERVER]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FtpServersPerCdnServer_CdnServer' AND object_id = OBJECT_ID('[dbo].[FtpServersPerCdnServer]'))
    CREATE NONCLUSTERED INDEX [IX_FtpServersPerCdnServer_CdnServer]
    ON [dbo].[FtpServersPerCdnServer] ([FK_CDNSERVER]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CdnServersPerVendor_CdnServer' AND object_id = OBJECT_ID('[dbo].[CdnServersPerVendor]'))
    CREATE NONCLUSTERED INDEX [IX_CdnServersPerVendor_CdnServer]
    ON [dbo].[CdnServersPerVendor] ([FK_CDNSERVER]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TIpologieTestiPerVendor_ResourceType' AND object_id = OBJECT_ID('[dbo].[TIpologieTestiPerVendor]'))
    CREATE NONCLUSTERED INDEX [IX_TIpologieTestiPerVendor_ResourceType]
    ON [dbo].[TIpologieTestiPerVendor] ([FK_RESOURCETYPE]);
