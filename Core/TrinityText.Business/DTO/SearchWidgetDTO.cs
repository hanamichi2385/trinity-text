namespace TrinityText.Business
{
    public class SearchTextDTO
    {
        public SearchTextDTO()
        {
        }

        public string Terms { get; set; }
        public string Site { get; set; }
        public string Website { get; set; }
        public int? TextTypeId { get; set; }
        public string[] LanguageIds { get; set; }
        public bool? ShowOnlyActive { get; set; }
        public bool? ShowOnlyDedicated { get; set; }

        public SortingType? SortingName { get; set; }
        public SortingType? SortingWebsite { get; set; }
        public SortingType? SortingSite { get; set; }
        //public SortingType? SortingContentType { get; set; }
        //public SortingType? SortingLastUpdate { get; set; }
        public SortingType? SortingLanguage { get; set; }

        //public IList<string> SupportedRoles { get; set; }

        public string[] UserWebsites { get; set; }

        public string[] WebsiteLanguages { get; set; }
    }
}
