namespace TrinityText.Business
{
    public class AWSCredentialSettingPayload : ICacheSettingPayload
    {
        public string DistributionId { get; set; }

        public string SecretKey { get; set; }

        public string AccessKeyId { get; set; }

        public string RegionName { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(DistributionId) &&
                !string.IsNullOrEmpty(SecretKey) &&
                !string.IsNullOrEmpty(AccessKeyId) &&
                !string.IsNullOrEmpty(RegionName);
        }
    }
}