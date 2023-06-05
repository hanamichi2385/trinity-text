using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using System;

namespace TrinityText.Domain.NH
{
    public class TrinityModelMapper : ModelMapper
    {
        public TrinityModelMapper()
        {
            AddDynamicMapping<CdnServer>(() =>
            {
                var classmapping = new ClassMapping<CdnServer>();
                classmapping.Schema("dbo");
                classmapping.Table("CdnServers");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.NAME, prop =>
                {
                    prop.Column("Nome");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.TYPE, prop =>
                {
                    prop.Column("Tipo");
                    prop.Type(NHibernateUtil.Int32);
                });
                classmapping.Property(e => e.BASEURL, prop =>
                {
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Bag(
                    x => x.CACHESETTINGS,
                    m =>
                    {
                        m.Key(n => n.Column("FK_CDNSERVER"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Inverse(true);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                    },
                    r => r.OneToMany()
                );
                classmapping.Bag(
                    x => x.CDNSERVERPERWEBSITES,
                    m =>
                    {
                        m.Key(n => n.Column("FK_CDNSERVER"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Inverse(true);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                    },
                    r => r.OneToMany()
                );
                classmapping.Bag(
                    x => x.FTPSERVERS,
                    m =>
                    {
                        m.Key(n => n.Column("FK_CDNSERVER"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Inverse(true);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                    },
                    r => r.OneToMany()
                );

                return classmapping;
            });

            AddDynamicMapping<CacheSettings>(() =>
            {
                var classmapping = new ClassMapping<CacheSettings>();
                classmapping.Schema("dbo");
                classmapping.Table("CacheSettings");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.PAYLOAD, prop =>
                {
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.TYPE, prop =>
                {
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_CDNSERVER, prop =>
                {
                    prop.Type(NHibernateUtil.Int32);
                    prop.Access(Accessor.Property);
                    prop.Insert(false);
                    prop.Update(false);
                });
                classmapping.ManyToOne(e => e.CDNSERVER, prop =>
                {
                    prop.Column("FK_CDNSERVER");
                    prop.Lazy(LazyRelation.NoLazy);
                    prop.NotNullable(true);
                });

                return classmapping;
            });

            AddDynamicMapping<CdnServersPerWebsite>(() =>
            {
                var classmapping = new ClassMapping<CdnServersPerWebsite>();
                classmapping.Schema("dbo");
                classmapping.Table("CdnServersPerVendor");
                classmapping.ComposedId(e =>
                {
                    e.ManyToOne(f => f.CDNSERVER);
                    e.Property(f => f.FK_WEBSITE);
                });
                classmapping.Property(e => e.FK_WEBSITE, m => m.Column("FK_VENDOR"));
                classmapping.Property(e => e.FK_CDNSERVER, 
                    m => {
                        m.Column("FK_CDNSERVER");
                        m.Insert(false);
                        m.Update(false);
                        m.Access(Accessor.Property);
                    });
                classmapping.ManyToOne(e => e.CDNSERVER, prop =>
                {
                    prop.Column("FK_CDNSERVER");
                    prop.Lazy(LazyRelation.NoLazy);
                    prop.NotNullable(true);
                    prop.Insert(false);
                    prop.Update(false);
                });

                return classmapping;
            });

            AddDynamicMapping<FtpServerPerCdnServer>(() =>
            {
                var classmapping = new ClassMapping<FtpServerPerCdnServer>();
                classmapping.Schema("dbo");
                classmapping.Table("FtpServersPerCdnServer");
                classmapping.ComposedId(e =>
                {
                    e.ManyToOne(f => f.FTPSERVER);
                    e.ManyToOne(f => f.CDNSERVER);
                });
                classmapping.ManyToOne(e => e.CDNSERVER, prop =>
                {
                    prop.Column("FK_CDNSERVER");
                    prop.Lazy(LazyRelation.NoLazy);
                    prop.NotNullable(true);
                    prop.Insert(false);
                    prop.Update(false);
                });
                classmapping.ManyToOne(e => e.FTPSERVER, prop =>
                {
                    prop.Column("FK_FTPSERVER");
                    prop.Lazy(LazyRelation.NoLazy);
                    prop.NotNullable(true);
                    prop.Insert(false);
                    prop.Update(false);
                });
                classmapping.Property(e => e.FK_CDNSERVER,
                    m => {
                        m.Column("FK_CDNSERVER");
                        m.Insert(false);
                        m.Update(false);
                        m.Access(Accessor.Property);
                    });
                classmapping.Property(e => e.FK_FTPSERVER,
                    m => {
                        m.Column("FK_FTPSERVER");
                        m.Insert(false);
                        m.Update(false);
                        m.Access(Accessor.Property);
                    });

                return classmapping;
            });

            AddDynamicMapping<FtpServer>(() =>
            {
                var classmapping = new ClassMapping<FtpServer>();
                classmapping.Schema("dbo");
                classmapping.Table("FtpServers");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.NAME, prop =>
                {
                    prop.Column("Nome");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.TYPE, prop =>
                {
                    prop.Column("Tipo");
                    prop.Type(NHibernateUtil.Int32);
                });
                classmapping.Property(e => e.PORT, prop =>
                {
                    prop.Type(NHibernateUtil.Int32);
                    prop.NotNullable(false);
                });
                classmapping.Property(e => e.HOST, prop =>
                {
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.USERNAME, prop =>
                {
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Bag(
                    x => x.CDNSERVERS,
                    m =>
                    {
                        m.Key(n => n.Column("FK_FTPSERVER"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Inverse(true);
                    },
                    r => r.OneToMany()
                );

                return classmapping;
            });

            AddDynamicMapping<File>(() =>
            {
                var classmapping = new ClassMapping<File>();
                classmapping.Schema("dbo");
                classmapping.Table("Files");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Guid);
                    id.Generator(Generators.Guid);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.CREATION_DATE, prop =>
                {
                    prop.Column("DATA_CREAZIONE");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.CREATION_USER, prop =>
                {
                    prop.Column("UTENTE_CREAZIONE");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.LASTUPDATE_DATE, prop =>
                {
                    prop.Column("DATA_ULTIMA_MODIFICA");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.LASTUPDATE_USER, prop =>
                {
                    prop.Column("UTENTE_ULTIMA_MODIFICA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.CONTENT, prop =>
                {
                    prop.Column("CONTENT");
                    prop.Type(NHibernateUtil.BinaryBlob);
                    prop.Length(int.MaxValue);
                });
                classmapping.Property(e => e.THUMBNAIL);
                classmapping.Property(e => e.FILENAME);
                classmapping.Property(e => e.FK_FOLDER);
                return classmapping;
            });

            AddDynamicMapping<Folder>(() =>
            {
                var classmapping = new ClassMapping<Folder>();
                classmapping.Schema("dbo");
                classmapping.Table("Cartelle");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_PARENT, prop =>
                {
                    prop.Column("PARENT_FOLDER");
                    prop.Type(NHibernateUtil.Int32);
                    prop.NotNullable(false);
                });
                classmapping.Property(e => e.DELETABLE, prop =>
                {
                    prop.Column("ELIMINABILE");
                    prop.Type(NHibernateUtil.Boolean);
                });
                classmapping.Property(e => e.NAME);
                classmapping.Property(e => e.NOTE);
                return classmapping;
            });

            AddDynamicMapping<PageType>(() =>
            {
                var classmapping = new ClassMapping<PageType>();
                classmapping.Schema("dbo");
                classmapping.Table("TipologieContenuti");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.NAME, prop =>
                {
                    prop.Column("NOME");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.PATH_PREVIEWPAGE, prop =>
                {
                    prop.Column("PATH_PAGINAPREVIEW");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.VISIBILITY, prop =>
                {
                    prop.Column("VISIBILITA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.SCHEMA, prop =>
                {
                    prop.Column("XMLSCHEMA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.OUTPUT_FILENAME);
                classmapping.Property(e => e.SUBFOLDER);
                classmapping.Property(e => e.PRINT_ELEMENT_NAME);
                classmapping.Bag(
                    x => x.PAGES,
                    m =>
                    {
                        m.Key(n => n.Column("FK_TIPOLOGIA"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Inverse(true);
                    },
                    r => r.OneToMany()
                );
                return classmapping;
            });

            AddDynamicMapping<Page>(() =>
                {
                    var classmapping = new ClassMapping<Page>();
                    classmapping.Schema("dbo");
                    classmapping.Table("Contenuti");
                    classmapping.BatchSize(100);
                    classmapping.Id(e => e.ID, id =>
                    {
                        id.Column("Id");
                        id.Type(NHibernateUtil.Int32);
                        id.Generator(Generators.Identity);
                    });
                    classmapping.Property(e => e.TITLE, prop =>
                    {
                        prop.Column("TITOLO");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.Property(e => e.CONTENT, prop =>
                    {
                        prop.Column("CONTENUTO");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.Property(e => e.FK_WEBSITE, prop =>
                    {
                        prop.Column("FK_VENDOR");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.Property(e => e.FK_PRICELIST, prop =>
                    {
                        prop.Column("FK_ISTANZA");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.Property(e => e.FK_PAGETYPE, prop =>
                    {
                        prop.Column("FK_TIPOLOGIA");
                        prop.Type(NHibernateUtil.Int32);
                        prop.NotNullable(true);
                    });
                    classmapping.Property(e => e.FK_LANGUAGE, prop =>
                    {
                        prop.Column("FK_LINGUA");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.Property(e => e.GENERATE_PDF, prop =>
                    {
                        prop.Column("GENERA_PDF");
                        prop.Type(NHibernateUtil.Boolean);
                    });
                    classmapping.Property(e => e.ACTIVE, prop =>
                    {
                        prop.Column("ATTIVA");
                        prop.Type(NHibernateUtil.Boolean);
                    });
                    classmapping.Property(e => e.CREATION_DATE, prop =>
                    {
                        prop.Column("DATA_CREAZIONE");
                        prop.Type(NHibernateUtil.DateTime);
                    });
                    classmapping.Property(e => e.CREATION_USER, prop =>
                    {
                        prop.Column("UTENTE_CREAZIONE");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.Property(e => e.LASTUPDATE_DATE, prop =>
                    {
                        prop.Column("DATA_ULTIMO_AGGIORNAMENTO");
                        prop.Type(NHibernateUtil.DateTime);
                    });
                    classmapping.Property(e => e.LASTUPDATE_USER, prop =>
                    {
                        prop.Column("UTENTE_ULTIMA_MODIFICA");
                        prop.Type(NHibernateUtil.String);
                    });
                    classmapping.ManyToOne(e => e.PAGETYPE, prop =>
                    {
                        prop.Column("FK_TIPOLOGIA");
                        prop.Lazy(LazyRelation.NoLazy);
                        prop.NotNullable(true);
                        prop.Insert(false);
                        prop.Update(false);
                    });

                    return classmapping;
                });

            AddDynamicMapping<Publication>(() =>
            {
                var classmapping = new ClassMapping<Publication>();
                classmapping.Schema("dbo");
                classmapping.Table("Generazioni");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.CREATION_USER, prop =>
                {
                    prop.Column("FK_UTENTE");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.LASTUPDATE_DATE, prop =>
                {
                    prop.Column("ULTIMO_AGGIORNAMENTO");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.STATUS_MESSAGE, prop =>
                {
                    prop.Column("STATUS");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FORMAT, prop =>
                {
                    prop.Column("TYPE");
                    prop.Type(NHibernateUtil.Int32);
                });
                classmapping.Property(e => e.FILTERDATA_DATE, prop =>
                {
                    prop.Column("DATA_FILTROGENERAZIONE_FILE");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.MANUALDELETE, prop =>
                {
                    prop.Column("PRESERVACOPIA");
                    prop.Type(NHibernateUtil.Boolean);
                });
                classmapping.Property(e => e.DATATYPE, prop =>
                {
                    prop.Column("TIPO_ESPORTAZIONE");
                    prop.Type(NHibernateUtil.Int32);
                });
                classmapping.ManyToOne(
                    x => x.CDNSERVER,
                    m =>
                    {
                        m.Column("FK_CDNSERVER");
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Lazy(LazyRelation.NoLazy);
                        m.NotNullable(false);
                    });
                classmapping.ManyToOne(
                    x => x.FTPSERVER,
                    m =>
                    {
                        m.Column("FK_FTPSERVER");
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Lazy(LazyRelation.NoLazy);
                        m.NotNullable(false);
                    });

                return classmapping;
            });

            AddDynamicMapping<Text>(() =>
            {
                var classmapping = new ClassMapping<Text>();
                classmapping.Schema("dbo");
                classmapping.Table("Risorse");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_PRICELIST, prop =>
                {
                    prop.Column("FK_ISTANZA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_COUNTRY, prop =>
                {
                    prop.Column("FK_NAZIONE");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_LANGUAGE, prop =>
                {
                    prop.Column("FK_LINGUA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_TEXTTYPE, prop =>
                {
                    prop.Column("FK_TIPOLOGIA");
                    prop.Type(NHibernateUtil.Int32);
                    prop.NotNullable(true);
                    prop.Access(Accessor.Property);
                    prop.Insert(false);
                    prop.Update(false);
                });

                classmapping.Property(e => e.ACTIVE, prop =>
                {
                    prop.Column("ATTIVA");
                    prop.Type(NHibernateUtil.Boolean);
                });
                classmapping.Property(e => e.NAME, prop =>
                {
                    prop.Column("NOME");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Bag(
                    x => x.REVISIONS,
                    m =>
                    {
                        m.Key(n => n.Column("RISORSA"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Inverse(true);
                    },
                    r => r.OneToMany()
                );
                classmapping.ManyToOne(
                    x => x.TEXTTYPE,
                    m =>
                    {
                        m.Column("FK_TIPOLOGIA");
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Lazy(LazyRelation.NoLazy);
                        m.NotNullable(true);
                    });

                return classmapping;
            });

            AddDynamicMapping<TextRevision>(() =>
            {
                var classmapping = new ClassMapping<TextRevision>();
                classmapping.Schema("dbo");
                classmapping.Table("TestiPerRisorsa");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_TEXT, prop =>
                {
                    prop.Column("RISORSA");
                    prop.Type(NHibernateUtil.Int32);
                    prop.Access(Accessor.Property);
                    prop.Insert(false);
                    prop.Update(false);
                });
                classmapping.Property(e => e.CREATION_DATE, prop =>
                {
                    prop.Column("DATA_CREAZIONE");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.REVISION_NUMBER, prop =>
                {
                    prop.Column("REVISIONE");
                    prop.Type(NHibernateUtil.Int32);
                });
                classmapping.Property(e => e.CONTENT, prop =>
                {
                    prop.Column("TESTO");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.CREATION_USER, prop =>
                {
                    prop.Column("UTENTE_CREAZIONE");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.ManyToOne(
                    x => x.TEXT,
                    m =>
                    {
                        m.Column("RISORSA");
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Lazy(LazyRelation.NoLazy);
                        m.NotNullable(true);
                    });

                return classmapping;
            });

            AddDynamicMapping<TextType>(() =>
            {
                var classmapping = new ClassMapping<TextType>();
                classmapping.Schema("dbo");
                classmapping.Table("TipologieTesti");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.CONTENTTYPE, prop =>
                {
                    prop.Column("TIPOLOGIA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.NOTE);
                classmapping.Property(e => e.SUBFOLDER);
                classmapping.Bag(
                    x => x.TEXTS,
                    m =>
                    {
                        m.Key(n => n.Column("FK_TIPOLOGIA"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Inverse(true);
                    },
                    r => r.OneToMany()
                );
                classmapping.Bag(
                    x => x.TEXTTYPEPERWEBSITES,
                    m =>
                    {
                        m.Key(n => n.Column("FK_RESOURCETYPE"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Inverse(true);
                    },
                    r => r.OneToMany()
                );

                return classmapping;
            });

            AddDynamicMapping<TextTypePerWebsite>(() =>
            {
                var classmapping = new ClassMapping<TextTypePerWebsite>();
                classmapping.Schema("dbo");
                classmapping.Table("TipologieTestiPerVendor");
                classmapping.ComposedId(e =>
                {
                    e.Property(a => a.FK_WEBSITE);
                    e.ManyToOne(a => a.TEXTTYPE);
                });
                classmapping.Property(e => e.FK_TEXTTYPE, prop =>
                {
                    prop.Column("FK_RESOURCETYPE");
                    prop.Type(NHibernateUtil.Int32);
                    prop.NotNullable(true);
                    prop.Access(Accessor.Property);
                    prop.Insert(false);
                    prop.Update(false);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                    prop.NotNullable(true);
                });
                classmapping.ManyToOne(
                    x => x.TEXTTYPE,
                    m =>
                    {
                        m.Column("FK_RESOURCETYPE");
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                        m.Lazy(LazyRelation.NoLazy);
                        m.NotNullable(true);
                    });


                return classmapping;
            });

            AddDynamicMapping<WebsiteConfiguration>(() =>
            {
                var classmapping = new ClassMapping<WebsiteConfiguration>();
                classmapping.Schema("dbo");
                classmapping.Table("WebsitesPerVendor");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.TYPE, prop =>
                {
                    prop.Column("TIPO");
                    prop.Type(NHibernateUtil.Int32);
                });

                return classmapping;
            });

            AddDynamicMapping<Widget>(() =>
            {
                var classmapping = new ClassMapping<Widget>();
                classmapping.Schema("dbo");
                classmapping.Table("WidgetContenuti");
                classmapping.Id(e => e.ID, id =>
                {
                    id.Column("Id");
                    id.Type(NHibernateUtil.Int32);
                    id.Generator(Generators.Identity);
                });
                classmapping.Property(e => e.FK_PRICELIST, prop =>
                {
                    prop.Column("FK_ISTANZA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_WEBSITE, prop =>
                {
                    prop.Column("FK_VENDOR");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.FK_LANGUAGE, prop =>
                {
                    prop.Column("FK_LINGUA");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.KEY, prop =>
                {
                    prop.Column("CHIAVE");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.CONTENT, prop =>
                {
                    prop.Column("CONTENUTO");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.CREATION_DATE, prop =>
                {
                    prop.Column("DATA_CREAZIONE");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.LASTUPDATE_DATE, prop =>
                {
                    prop.Column("DATA_ULTIMA_MODIFICA");
                    prop.Type(NHibernateUtil.DateTime);
                });
                classmapping.Property(e => e.CREATION_USER, prop =>
                {
                    prop.Column("UTENTE_CREAZIONE");
                    prop.Type(NHibernateUtil.String);
                });
                classmapping.Property(e => e.LASTUPDATE_USER, prop =>
                {
                    prop.Column("UTENTE_ULTIMA_MODIFICA");
                    prop.Type(NHibernateUtil.String);
                });
                return classmapping;
            });
        }

        public void AddDynamicMapping<T>(Func<ClassMapping<T>> func) where T : class
        {
            var mapping = func.Invoke();
            AddMapping(mapping);
        }
    }
}
