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
                    e.Property(f => f.FK_CDNSERVER);
                    e.Property(f => f.FK_WEBSITE);
                });
                classmapping.Property(e => e.FK_WEBSITE, m => m.Column("FK_VENDOR"));

                return classmapping;
            });

            AddDynamicMapping<FtpServerPerCdnServer>(() =>
            {
                var classmapping = new ClassMapping<FtpServerPerCdnServer>();
                classmapping.Schema("dbo");
                classmapping.Table("FtpServersPerCdnServer");
                classmapping.ComposedId(e =>
                {
                    e.Property(f => f.FK_CDNSERVER);
                    e.Property(f => f.FK_FTPSERVER);
                });
                //classmapping.ManyToOne(e => e.CDNSERVER, prop =>
                //{
                //    prop.Column("FK_CDNSERVER");
                //    prop.Lazy(LazyRelation.NoLazy);
                //    prop.NotNullable(true);
                //});
                //classmapping.Property(e => e.CDNSERVER, prop =>
                //{

                //    prop.Column("FK_CDNSERVER");
                //    //prop.ForeignKey("FK_CDNSERVER");
                //    //prop.Constrained(true);
                //    //prop.Lazy(LazyRelation.NoLazy);
                //    //prop.NotNullable(true);
                //});
                //classmapping.Property(e => e.FTPSERVER, prop =>
                //{
                //    prop.Column("FK_FTPSERVER");
                //    //prop.Constrained(true);
                //    //prop.Lazy(LazyRelation.NoLazy);
                //    //prop.NotNullable(true);
                //});

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
                        m.Key(n => n.Column("FK_CDNSERVER"));
                        m.Lazy(CollectionLazy.NoLazy);
                        m.Cascade(NHibernate.Mapping.ByCode.Cascade.DeleteOrphans);
                    },
                    r => r.OneToMany()
                );

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
