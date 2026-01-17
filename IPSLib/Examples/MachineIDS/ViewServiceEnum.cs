using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.Examples.MachineIDS
{
    public class StringValueAttribute : Attribute
    {
        public string Value { get; }
        public StringValueAttribute(string value)
        {
            Value = value;
        }
    }
    public enum ViewServiceEnum
    {
        [Description("Все данные, которые может поставить view_service")]
        [StringValue("total")]
        TOTAL,

        [Description("Статистика использования cpu и процессов")]
        [StringValue("cpu")]
        CPU,

        [Description("Статистика использования cpu диска")]
        [StringValue("disk")]
        DISK,

        [Description("Статистика сетевого взаимодействия")]
        [StringValue("network")]
        NETWORK
    }
    public static class EnumExtensions
    {
        public static string GetStringValue(this Enum value)
        {
            var type = value.GetType();
            var fieldInfo = type.GetField(value.ToString());
            if (fieldInfo != null)
            {
                var attributes = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false);
                if (attributes.Length > 0)
                {
                    return ((StringValueAttribute)attributes[0]).Value;
                }
            }
            return value.ToString(); // Возвращаем имя по умолчанию, если атрибута нет
        }
    }
}
