using Microsoft.EntityFrameworkCore;

namespace TrinityText.Domain.EF
{
    public class TrinityDbContext : DbContext
    {
        public TrinityDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<CdnServer>(entity =>
            {
                entity
                    .ToTable(name: "CdnServers")
                    .HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.NAME).HasColumnName("Nome");
                entity.Property(e => e.TYPE).HasColumnName("Tipo");
                entity.HasMany(e => e.CACHESETTINGS)
                    .WithOne(e => e.CDNSERVER)
                    .HasForeignKey(e => e.FK_CDNSERVER);

                entity.HasMany(e => e.CDNSERVERPERWEBSITES)
                    .WithOne(e => e.CDNSERVER)
                    .HasForeignKey(e => e.FK_CDNSERVER);

                entity.HasMany(e => e.PUBLICATIONS)
                    .WithOne(e => e.CDNSERVER)
                    .HasForeignKey(e => e.FK_CDNSERVER);

                entity.HasMany(e => e.FTPSERVERS)
                    .WithOne(e => e.CDNSERVER)
                    .HasForeignKey(e => e.FK_CDNSERVER);
            });

            builder.Entity<CacheSettings>(entity =>
            {
                entity.ToTable(name: "CacheSettings").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity
                     .HasOne(e => e.CDNSERVER)
                     .WithMany(s => s.CACHESETTINGS)
                     .HasForeignKey(s => s.FK_CDNSERVER);
            });
            

            builder.Entity<CdnServersPerWebsite>(entity =>
            {
                entity.ToTable(name: "CdnServersPerVendor").HasKey(e => new {
                    e.FK_CDNSERVER,
                    e.FK_WEBSITE
                });
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
            });

            builder.Entity<FtpServerPerCdnServer>(entity =>
            {
                entity.ToTable(name: "FtpServersPerCdnServer").HasKey(e => new {
                    e.FK_CDNSERVER,
                    e.FK_FTPSERVER
                });
            });

            builder.Entity<FtpServer>(entity =>
            {
                entity.ToTable(name: "FtpServers").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.NAME).HasColumnName("Nome");
                entity.Property(e => e.TYPE).HasColumnName("Tipo");

                entity.HasMany(e => e.CDNSERVERS)
                    .WithOne(e => e.FTPSERVER)
                    .HasForeignKey(e => e.FK_FTPSERVER);
            });

            builder.Entity<File>(entity =>
            {
                entity.ToTable(name: "Files").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.CREATION_DATE).HasColumnName("DATA_CREAZIONE");
                entity.Property(e => e.CREATION_USER).HasColumnName("UTENTE_CREAZIONE");
                entity.Property(e => e.LASTUPDATE_DATE).HasColumnName("DATA_ULTIMA_MODIFICA");
                entity.Property(e => e.LASTUPDATE_USER).HasColumnName("UTENTE_ULTIMA_MODIFICA");

                entity.Property(e => e.CONTENT).HasMaxLength(int.MaxValue);

                entity
                    .HasOne(e => e.FOLDER)
                    .WithMany(s => s.FILES)
                    .HasForeignKey(s => s.FK_FOLDER);
            });

            builder.Entity<Folder>(entity =>
            {
                entity.ToTable(name: "Cartelle").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
                entity.Property(e => e.FK_PARENT).HasColumnName("PARENT_FOLDER");
                entity.Property(e => e.DELETABLE).HasColumnName("ELIMINABILE");

                entity
                    .HasOne(e => e.PARENT)
                    .WithMany(s => s.SUBFOLDERS)
                    .HasForeignKey(s => s.FK_PARENT);

                entity.HasMany(e => e.SUBFOLDERS)
                   .WithOne(e => e.PARENT)
                   .HasForeignKey(e => e.FK_PARENT);

                entity.HasMany(e => e.FILES)
                   .WithOne(e => e.FOLDER)
                   .HasForeignKey(e => e.FK_FOLDER);
            });

            builder.Entity<Page>(entity =>
            {
                entity.ToTable(name: "Contenuti").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
                entity.Property(e => e.ACTIVE).HasColumnName("ATTIVA");
                entity.Property(e => e.CONTENT).HasColumnName("CONTENUTO");
                entity.Property(e => e.CREATION_DATE).HasColumnName("DATA_CREAZIONE");
                entity.Property(e => e.LASTUPDATE_DATE).HasColumnName("DATA_ULTIMO_AGGIORNAMENTO");
                entity.Property(e => e.FK_LINGUAGE).HasColumnName("FK_LINGUA");
                entity.Property(e => e.FK_PRICELIST).HasColumnName("FK_ISTANZA");
                entity.Property(e => e.GENERATE_PDF).HasColumnName("GENERA_PDF");
                entity.Property(e => e.TITLE).HasColumnName("TITOLO");
                entity.Property(e => e.CREATION_USER).HasColumnName("UTENTE_CREAZIONE");
                entity.Property(e => e.LASTUPDATE_USER).HasColumnName("UTENTE_ULTIMA_MODIFICA");
                entity.Property(e => e.FK_PAGETYPE).HasColumnName("FK_TIPOLOGIA");

                entity
                    .HasOne(e => e.PAGETYPE)
                    .WithMany(s => s.PAGES)
                    .HasForeignKey(s => s.FK_PAGETYPE);
            });

            builder.Entity<PageType>(entity =>
            {
                entity.ToTable(name: "TipologieContenuti").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
                entity.Property(e => e.NAME).HasColumnName("NOME");
                entity.Property(e => e.PATH_PREVIEWPAGE).HasColumnName("PATH_PAGINAPREVIEW");
                entity.Property(e => e.VISIBILITY).HasColumnName("VISIBILITA");
                entity.Property(e => e.SCHEMA).HasColumnName("XMLSCHEMA");

                entity.HasMany(e => e.PAGES)
                    .WithOne(e => e.PAGETYPE)
                    .HasForeignKey(e => e.FK_PAGETYPE);
            });

            builder.Entity<Publication>(entity =>
            {
                entity.ToTable(name: "Generazioni").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();

                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
                entity.Property(e => e.FILTERDATA_DATE).HasColumnName("DATA_FILTROGENERAZIONE_FILE");
                entity.Property(e => e.CREATION_USER).HasColumnName("FK_UTENTE");
                entity.Property(e => e.MANUALDELETE).HasColumnName("PRESERVACOPIA");
                entity.Property(e => e.PUBLICATIONTYPE).HasColumnName("TIPO_ESPORTAZIONE");
                entity.Property(e => e.LASTUPDATE_DATE).HasColumnName("ULTIMO_AGGIORNAMENTO");

                entity
                   .HasOne(e => e.CDNSERVER)
                   .WithMany(s => s.PUBLICATIONS)
                   .HasForeignKey(s => s.FK_CDNSERVER);

                entity
                   .HasOne(e => e.FTPSERVER)
                   .WithMany(s => s.PUBLICATIONS)
                   .HasForeignKey(s => s.FK_FTPSERVER);
            });

            builder.Entity<Text>(entity =>
            {
                entity
                    .ToTable(name: "Risorse")
                    .HasKey(e => e.ID);
                entity
                    .Property(e => e.ID).ValueGeneratedOnAdd();

                entity.Property(e => e.FK_PRICELIST).HasColumnName("FK_ISTANZA");
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
                entity.Property(e => e.FK_COUNTRY).HasColumnName("FK_NAZIONE");
                entity.Property(e => e.FK_LANGUAGE).HasColumnName("FK_LINGUA");
                entity.Property(e => e.FK_TEXTTYPE).HasColumnName("FK_TIPOLOGIA");
                entity.Property(e => e.ACTIVE).HasColumnName("ATTIVA");
                entity.Property(e => e.NAME).HasColumnName("NOME");
                

                entity
                    .HasOne(e => e.TEXTTYPE)
                    .WithMany(s => s.TEXTS)
                    .HasForeignKey(s => s.FK_TEXTTYPE);
            });

            builder.Entity<TextRevision>(entity =>
            {
                entity
                    .ToTable(name: "TestiPerRisorsa")
                    .HasKey(e => e.ID);
                entity
                    .Property(e => e.ID).ValueGeneratedOnAdd();

                entity.Property(e => e.FK_TEXT).HasColumnName("RISORSA");
                entity.Property(e => e.CREATION_DATE).HasColumnName("DATA_CREAZIONE");
                entity.Property(e => e.REVISION_NUMBER).HasColumnName("REVISIONE");
                entity.Property(e => e.CONTENT).HasColumnName("TESTO");
                entity.Property(e => e.CREATION_USER).HasColumnName("UTENTE_CREAZIONE");

                entity
                    .HasOne(e => e.TEXT)
                    .WithMany(s => s.REVISIONS)
                    .HasForeignKey(s => s.FK_TEXT);
            });

            builder.Entity<TextType>(entity =>
            {
                entity.ToTable(name: "TipologieTesti").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.CONTENTTYPE).HasColumnName("TIPOLOGIA");

                entity.HasMany(e => e.TEXTS)
                    .WithOne(e => e.TEXTTYPE)
                    .HasForeignKey(e => e.FK_TEXTTYPE);
                entity.HasMany(e => e.TEXTTYPEPERWEBSITES)
                    .WithOne(e => e.TEXTTYPE)
                    .HasForeignKey(e => e.FK_TEXTTYPE);
            });

            builder.Entity<TextTypePerWebsite>(entity =>
            {
                entity.ToTable(name: "TipologieTestiPerVendor").HasKey(e => new {
                    e.FK_WEBSITE,
                    e.FK_TEXTTYPE
                });

                entity.Property(e => e.FK_TEXTTYPE).HasColumnName("FK_RESOURCETYPE");
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");

                entity
                    .HasOne(e => e.TEXTTYPE)
                    .WithMany(s => s.TEXTTYPEPERWEBSITES)
                    .HasForeignKey(s => s.FK_TEXTTYPE);
            });

            builder.Entity<WebsiteConfiguration>(entity =>
            {
                entity.ToTable(name: "WebsitesPerVendor").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
            });

            builder.Entity<Widget>(entity =>
            {
                entity.ToTable(name: "WidgetContenuti").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();

                entity.Property(e => e.KEY).HasColumnName("CHIAVE");
                entity.Property(e => e.CONTENT).HasColumnName("CONTENUTO");
                entity.Property(e => e.CREATION_DATE).HasColumnName("DATA_CREAZIONE");
                entity.Property(e => e.LASTUPDATE_DATE).HasColumnName("DATA_ULTIMA_MODIFICA");
                entity.Property(e => e.FK_LANGUAGE).HasColumnName("FK_LINGUA");
                entity.Property(e => e.FK_PRICELIST).HasColumnName("FK_ISTANZA");
                entity.Property(e => e.FK_WEBSITE).HasColumnName("FK_VENDOR");
                entity.Property(e => e.CREATION_USER).HasColumnName("UTENTE_CREAZIONE");
                entity.Property(e => e.LASTUPDATE_USER).HasColumnName("UTENTE_ULTIMA_MODIFICA");
            });
        }
    }
}
