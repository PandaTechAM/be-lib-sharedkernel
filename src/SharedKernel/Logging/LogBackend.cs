namespace SharedKernel.Logging;

public enum LogBackend
{
   None = 1,
   ElasticSearch = 2,
   Loki = 3,
   CompactJson = 4
}