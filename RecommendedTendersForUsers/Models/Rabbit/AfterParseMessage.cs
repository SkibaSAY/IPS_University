using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendedTendersForUsers.Models.Rabbit
{
    /// <summary>
    /// IdTypeRabbitMessage в других проектах, но это создаёт путаницу
    /// </summary>
    public class AfterParseMessage
    {
        [JsonProperty(PropertyName = "id")]
        public long Id { get; set; }

        [JsonProperty(PropertyName = "reg_number")]
        public string RegNumber { get; set; }

        [JsonProperty(PropertyName = "type")]
        public EntityType Type { get; set; }
    }
}
