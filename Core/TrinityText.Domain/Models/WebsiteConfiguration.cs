namespace TrinityText.Domain
{
    public partial class WebsiteConfiguration
    {
        public virtual int ID { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual int TYPE { get; set; }
        public virtual string URL { get; set; }
        public virtual string NOTE { get; set; }
    }
}
