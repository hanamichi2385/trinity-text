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

                entity
                    .HasOne(e => e.PAGETYPE)
                    .WithMany(s => s.PAGES)
                    .HasForeignKey(s => s.FK_PAGETYPE);
            });

            builder.Entity<PageType>(entity =>
            {
                entity.ToTable(name: "TipologieContenuti").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();

                entity.HasMany(e => e.PAGES)
                    .WithOne(e => e.PAGETYPE)
                    .HasForeignKey(e => e.FK_PAGETYPE);
            });

            builder.Entity<Publication>(entity =>
            {
                entity.ToTable(name: "Generazioni").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();

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
                entity
                    .HasOne(e => e.TEXT)
                    .WithMany(s => s.REVISIONS)
                    .HasForeignKey(s => s.FK_TEXT);
            });

            builder.Entity<TextType>(entity =>
            {
                entity.ToTable(name: "TipologieTesti").HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.HasMany(e => e.TEXTS)
                    .WithOne(e => e.TEXTTYPE)
                    .HasForeignKey(e => e.FK_TEXTTYPE);
                entity.HasMany(e => e.TEXTTYPEPERWEBSITES)
                    .WithOne(e => e.TEXTTYPE)
                    .HasForeignKey(e => e.FK_TEXTTYPE);
            });

            //builder.Entity<TextType>(entity =>
            //{
            //    entity.ToTable(name: "Operators").HasKey(e => e.ID);
            //    entity.Property(e => e.ID).ValueGeneratedOnAdd();
            //    entity.HasMany(e => e.TEXTS)
            //        .WithOne(e => e.TEXTTYPE)
            //        .HasForeignKey(e => e.FK_TEXTTYPE);
            //    entity.HasMany(e => e.TEXTTYPEPERWEBSITES)
            //        .WithOne(e => e.TEXTTYPE)
            //        .HasForeignKey(e => e.FK_TEXTTYPE);
            //});

            builder.Entity<TextTypePerWebsite>(entity =>
            {
                entity.ToTable(name: "TipologieTestiPerVendor").HasKey(e => new {
                    e.FK_VENDOR,
                    e.FK_TEXTTYPE
                });

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
            });
        }
    }
}
