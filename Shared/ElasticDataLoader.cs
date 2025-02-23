using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class ElasticDataLoader
    {
        public class ElasticConnectionParams
        {
            public string Host;
            public string Login;
            public string Password;
        }

        ElasticClient Elastic;
        public ElasticDataLoader(string connectionFilePath, string connectionName, string defaultIndex = "log*")
        {
            var connectionParams = ReadConnection(connectionFilePath,connectionName);
            IConnectionPool connectionPool;
            connectionPool = new SingleNodeConnectionPool(new Uri(connectionParams.Host));

            //найти способ передать индекс по умолчанию
            var connectionSettings = new ConnectionSettings(connectionPool, sourceSerializer: (builtin, settings) => new JsonNetSerializer(builtin, settings, () => new JsonSerializerSettings() { DateParseHandling = DateParseHandling.DateTimeOffset, DateTimeZoneHandling = DateTimeZoneHandling.Utc }))
                .BasicAuthentication(connectionParams.Login, connectionParams.Password)
                .RequestTimeout(new TimeSpan(0, 5, 0))
                .DefaultIndex(defaultIndex)
                .EnableApiVersioningHeader()
                .ServerCertificateValidationCallback((sender, cert, chain, errors) => true)
                .DisableDirectStreaming(true);
            //

            Elastic = new ElasticClient(connectionSettings);
        }

        public SearchResponse<T> GetResponce<T>(string queryString) where T:class
        {
            var postData = PostData.String(queryString);

            var response = Elastic.LowLevel.Search<SearchResponse<T>>(postData, null);

            return response;
        }

        static Regex connectionRegex = new Regex("(?<name>\".+?\"):\"Host=(?<host>.+?);Login=(?<login>.+?);Password=(?<password>.+?)\"", RegexOptions.Compiled);
        private ElasticConnectionParams ReadConnection(string connectionFilePath, string connectionName)
        {
            var connectionResult = new ElasticConnectionParams();
            if (connectionName == null || connectionName.Length == 0)
            {
                throw new NullReferenceException("Connection name must not null or empty.");
            }

            if (connectionFilePath == null || connectionFilePath.Length == 0)
            {
                throw new NullReferenceException("Connection file must not null or empty.");
            }
            
            string[] array = File.ReadAllLines(connectionFilePath, Encoding.UTF8);
            string[] array2 = array;
            foreach (string input in array2)
            {
                Match match = connectionRegex.Match(input);
                if (!match.Success)
                {
                    continue;
                }

                if (connectionName != null)
                {
                    if (match.Groups["name"].Value.Trim(new char[1] { '"' }) == connectionName)
                    {
                        connectionResult.Host = match.Groups["host"].Value;
                        connectionResult.Login = match.Groups["login"].Value;
                        connectionResult.Password = match.Groups["password"].Value;
                        break;
                    }

                    continue;
                }
            }
            return connectionResult;
        }
    }
}
