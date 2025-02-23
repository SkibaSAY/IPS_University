using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using Nest;
using Newtonsoft.Json;
using SharedLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher
{
    public class EstimationsByElastic : EstimateSearcherBase
    {
        public EstimationsByElastic(PgClient pgClient):base(pgClient)
        {

        }
        /// <summary>
        /// Количество открытий тендера, после которого мы считаем тендер интересным
        /// </summary>
        public const int OpenCountByInteresting = 3;

        public override List<EntityEstimation> FindEstimations(long userId)
        {
            var tendersThatUserOpen = GetTendersThatUserOpen(userId);

            //отбираем те, которые открывали  больше 3х раз
            var openMore = tendersThatUserOpen.Where(e => e.CountOfOpen > OpenCountByInteresting);
            var result = EntityEstimation.GetByTenderIds(PostgreClient, openMore.Select(t => t.TenderId));
            //EntityEstimation.FillEstimationsFromBaseTables(PostgreClient, result);
            result.ForEach(e => e.Estimation = (int)EstimateScoreEnum.MostView);

            return result;
        }

        public class TenderUserOpen
        {
            public TenderUserOpen() { }
            public long TenderId { get; set; }
            public long? CountOfOpen { get; set; } 
        }

        private static ElasticDataLoader Elastic = new ElasticDataLoader("C:\\Publish\\connections.txt", "elastic_data_client");
        public List<TenderUserOpen> GetTendersThatUserOpen(long userId)
        {
            var lteDate = DateTime.UtcNow;
            var lteDateStr = JsonConvert.SerializeObject(lteDate).Replace("\"", "");
            var gteDate = lteDate.AddMonths(-3);
            var gteDateStr = JsonConvert.SerializeObject(gteDate).Replace("\"", "");

            var query = @$"
            {{
              ""aggs"": {{
                ""0"": {{
                  ""terms"": {{
                    ""field"": ""fields.entity_id"",
                    ""order"": {{
                      ""_count"": ""desc""
                    }},
                    ""size"": 1000
                  }}
                }}
              }},
              ""size"": 0,
              ""fields"": [
                {{
                  ""field"": ""@timestamp"",
                  ""format"": ""date_time""
                }}
              ],
              ""script_fields"": {{}},
              ""stored_fields"": [
                ""*""
              ],
              ""runtime_mappings"": {{}},
              ""_source"": {{
                ""excludes"": []
              }},
              ""query"": {{
                ""bool"": {{
                  ""must"": [],
                  ""filter"": [
                    {{
                      ""bool"": {{
                        ""should"": [
                          {{
                            ""match"": {{
                              ""fields.user_id"": ""{userId}""
                            }}
                          }}
                        ],
                        ""minimum_should_match"": 1
                      }}
                    }},
                    {{
                        ""bool"": {{
                            ""should"": [
                            {{
                                ""match"": {{
                                ""app"": ""asptenderland""
                                }}
                            }}
                            ],
                            ""minimum_should_match"": 1
                        }}
                    }},
                    {{
                        ""bool"": {{
                            ""should"": [
                            {{
                                ""match"": {{
                                ""fields.entity_type"": ""1""
                                }}
                            }}
                            ],
                            ""minimum_should_match"": 1
                        }}
                    }},
                    {{
                      ""range"": {{
                        ""@timestamp"": {{
                          ""format"": ""strict_date_optional_time"",
                          ""gte"": ""{gteDateStr}"",
                          ""lte"": ""{lteDateStr}""
                        }}
                      }}
                    }}
                  ],
                  ""should"": [],
                  ""must_not"": []
                }}
              }}
            }}";

            var responce = Elastic.GetResponce<TenderUserOpen>(query);

            var result = (responce.Aggregations.Values.ToArray()[0] as BucketAggregate).Items.Select(i =>
            {
                var bucket = i as KeyedBucket<object>;
                return new TenderUserOpen { TenderId = (long)bucket.Key, CountOfOpen = bucket.DocCount };
            }).ToList();
            return result;
        }
    }
}
