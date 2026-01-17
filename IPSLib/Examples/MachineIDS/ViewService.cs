using Elasticsearch.Net;
using IPSLib.Examples.MachineIDS.expresions;
using Microsoft.Data.Analysis;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScottPlot.TickGenerators.TimeUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPSLib.Examples.MachineIDS
{
    /// <summary>
    /// Сервис представления
    /// </summary>
    public class ViewService
    {
        static HttpClient HttpClient = new HttpClient();
        protected string ServiceUrl;
        protected ViewServiceEnum ViewType;
        public ViewService(ViewServiceEnum viewType, string url = "http://127.0.0.1:8001")
        {
            this.ServiceUrl = url;
            this.ViewType = viewType;
        }

        public DataFrame GetLearning(int hour)
        {
            var view_type = ViewType.GetStringValue();
            var form = new {
                hour = hour,
                view_type = view_type
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(form);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = HttpClient.PostAsync($"{ServiceUrl}/get_learning", content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            JArray learningDataJson = (JArray)JObject.Parse(responseString).GetValue("learning_data");

            var dataFrame = learningDataJson.ToDataFrame();
            return dataFrame;
        }
        public DataFrame GetActual(DateTime dateTime)
        {
            var view_type = ViewType.GetStringValue();
            var form = new
            {
                date_time = dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                view_type = view_type
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(form);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = HttpClient.PostAsync($"{ServiceUrl}/get_actual", content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            JArray lastItemsJson = (JArray)JObject.Parse(responseString).GetValue("last_items");

            var dataFrame = lastItemsJson.ToDataFrame();
            return dataFrame;
        }
    }
}
