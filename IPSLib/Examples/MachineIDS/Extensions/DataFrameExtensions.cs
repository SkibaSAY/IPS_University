using CsvHelper;
using Microsoft.Data.Analysis;
using Newtonsoft.Json.Linq;
using ScottPlot.MultiplotLayouts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Formats.Asn1;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.Examples.MachineIDS.expresions
{
    public static class DataFrameExtensions
    {
        public static DataFrame ToDataFrame(this JArray jsonArray)
        {
            //var dataFrame = new DataFrame();
            //var columns = new Dictionary<string, Type>();

            //foreach (JObject jsonItem in jsonArray)
            //{
            //    DataFrameRow row = 
            //    foreach (JProperty field in jsonItem.Children())
            //    {
            //        var a = field.Value;
            //        var type = a.Type;
            //        if (columns.ContainsKey(field.Name))
            //        {

            //        }
            //          //  dataFrame.Columns.Add(new DataFrameColumn(""));
            //    }
            //}
            //foreach (DataColumn column in dataTable.Columns)
            //{
            //    // Create a list to hold the column values
            //    var columnValues = new List<object>();

            //    foreach (DataRow row in dataTable.Rows)
            //    {
            //        columnValues.Add(row[column.ColumnName]);
            //    }

            //    // The type of the DataFrameColumn must match the actual data type of the values
            //    // The following uses the column's native data type.
            //    var dataFrameColumn = DataFrameColumn.Create(column.ColumnName, columnValues);
            //    dataFrame.Columns.Add(dataFrameColumn);
            //}

            //return dataFrame;

            //DataFrame dataFrame = null;
            //var records = jsonArray.Select(item => {
            //    var obj = new ExpandoObject();
            //    foreach(JProperty field in item.Children())
            //    {
            //        obj.TryAdd(field.Name, field.Value);
            //    }
            //    return obj;

            //}).ToList();

            //using (var writer = new StringWriter())
            //using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            //{
            //    // Write the header and the records
            //    // CsvHelper dynamically generates headers from the property names of the objects
            //    csv.WriteRecords(records);
            //    dataFrame = DataFrame.LoadCsvFromString(writer.ToString());
            //}
            //return dataFrame;

            DataFrame dataFrame = new DataFrame();
            var records = jsonArray.Select(item =>
            {
                var obj = new ExpandoObject();
                foreach (JProperty field in item.Children())
                {
                    obj.TryAdd(field.Name, field.Value);
                }
                return obj;

            }).ToList();

            //т.е. у нас тут пустой результат
            if (records.Count == 0)
            {
                return dataFrame;
            }

            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                var columns = jsonArray.SelectMany(item => item.Children())
                        .DistinctBy(item => ((JProperty)item).Name)
                        .ToList();

                //заполнили шапку
                foreach (JProperty prop in columns)
                {
                    csv.WriteField(prop.Name);
                }
                //переключились на строки
                csv.NextRecord();
                
                //заполняет строки
                foreach (var item in jsonArray)
                {
                    foreach (JProperty prop in columns)
                    {
                        // Write the value if the key exists, otherwise an empty string
                        var value = item[prop.Name]?.ToString() ?? "";
                        csv.WriteField(value);
                    }
                    csv.NextRecord();
                }

                dataFrame = DataFrame.LoadCsvFromString(writer.ToString());
            }
            return dataFrame;
        }
    }
}
