-- Optional schema changes that need a look at the data first. Run them manually, one at a time, on a copy of production first.

-- 1) Contenuti.CONTENUTO is "ntext": deprecated type, always stored out of the row (extra read for every page).
--    nvarchar(max) is the drop-in replacement (the column is copied: schedule it in a quiet period on large tables).
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID('[dbo].[Contenuti]') AND c.name = 'CONTENUTO' AND t.name = 'ntext')
BEGIN
    DECLARE @nullable bit = (SELECT is_nullable FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Contenuti]') AND name = 'CONTENUTO');
    IF @nullable = 1
        ALTER TABLE [dbo].[Contenuti] ALTER COLUMN [CONTENUTO] nvarchar(max) NULL;
    ELSE
        ALTER TABLE [dbo].[Contenuti] ALTER COLUMN [CONTENUTO] nvarchar(max) NOT NULL;
END
GO

-- 2) A text can have only one revision with a given number. Check for duplicates first:
--    SELECT RISORSA, REVISIONE, COUNT(*) FROM [dbo].[TestiPerRisorsa] GROUP BY RISORSA, REVISIONE HAVING COUNT(*) > 1;
--    (fix them, then) replace the plain index of indexes.sql with a unique one:
IF NOT EXISTS (SELECT 1 FROM [dbo].[TestiPerRisorsa] GROUP BY RISORSA, REVISIONE HAVING COUNT(*) > 1)
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TestiPerRisorsa_Risorsa_Revisione' AND object_id = OBJECT_ID('[dbo].[TestiPerRisorsa]'))
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TestiPerRisorsa_Risorsa_Revisione' AND object_id = OBJECT_ID('[dbo].[TestiPerRisorsa]'))
        DROP INDEX [IX_TestiPerRisorsa_Risorsa_Revisione] ON [dbo].[TestiPerRisorsa];

    CREATE UNIQUE NONCLUSTERED INDEX [UX_TestiPerRisorsa_Risorsa_Revisione]
    ON [dbo].[TestiPerRisorsa] ([RISORSA], [REVISIONE]);
END
GO
