namespace HGTSWebApi.Services
{
    public class CapacityCacheOptions
    {
        public string Provider { get; set; } = "InMemory";
        public string RedisConfiguration { get; set; } = string.Empty;
        public string RedisInstanceName { get; set; } = "hgts:";
    }
}
