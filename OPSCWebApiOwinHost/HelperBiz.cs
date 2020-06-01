using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Reflection;
using System.Collections.Specialized;

using System.Collections;
using System.Web.Script.Serialization;

namespace JNRSWebApiOwinHost
{
    public class HelperBiz
    {
        public HelperBiz()
        {

        }
        public List<T> ConvertList<T>(DataTable dt)
        {
            List<T> result = new List<T>();

            // 定义Converter委托
            Converter<DataRow, T> converter = delegate(DataRow row)
            {
                // 创建泛型对象
                T rowInstance = Activator.CreateInstance<T>();

                // 获取泛型对象的属性集合
                PropertyInfo[] properties = typeof(T).GetProperties();

                // 通过反射遍历对象的每个属性
                foreach (PropertyInfo property in properties)
                {
                    // 判断是否存在以属性名称为字段名称的列，若存在则对泛型对象的属性进行赋值
                    if (row.Table.Columns.Contains(property.Name))
                    {
                        // 注意需要将值转换成属性相同的类型
                        if (row[property.Name].GetType().ToString() == "System.DBNull")
                            continue;
                        //property.SetValue(rowInstance, Convert.ChangeType(row[property.Name], property.PropertyType), null);
                        property.SetValue(rowInstance, row[property.Name], null);
                    }
                }

                return rowInstance;
            };

            // 这里和Array.ConvertAll的实现方式相同
            foreach (DataRow row in dt.Rows)
            {
                result.Add(converter(row));
            }

            return result;
        }

        public object ConvertJson(DataTable dt)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = Int32.MaxValue; //取得最大数值
            ArrayList arrayList = new ArrayList();
            foreach (DataRow dataRow in dt.Rows)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();  //实例化一个参数集合
                foreach (DataColumn dataColumn in dt.Columns)
                {
                    dictionary.Add(dataColumn.ColumnName, dataRow[dataColumn.ColumnName].ToString());
                }
                arrayList.Add(dictionary); //ArrayList集合中添加键值
            }
            return javaScriptSerializer.Deserialize<object>(javaScriptSerializer.Serialize(arrayList));

        }
        //返回一
        public List<string> GetPropertyList(object obj)
        {
            List<string> propertyList = new List<string>();

            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            string p;
            foreach (PropertyInfo property in properties)
            {
                object o = property.GetValue(obj, null);
                if (o == null)
                    p = "";
                else if (o != null && o.GetType() == typeof(bool))
                    p = (bool)o ? "是" : "否";
                else
                    p = o.ToString();
                //propertyList.Add(o == null ? "" : o.ToString());
                propertyList.Add(p);
            }

            return propertyList;
        }
    }
}