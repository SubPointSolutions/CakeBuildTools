using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SubPointSolutions.CakeBuildTools.Utils
{
    public static class DynamicUtils
    {
        public static dynamic ToDynamic(this object obj)
        {
            if (obj == null)
                throw new Exception("obj is null");

            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                // hidden?
                // IgnoreDataMember or XmlIgnore?
                var xmlIgnore = propertyInfo.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Length > 0;
                var ignoreDataMember = propertyInfo.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Length > 0;

                var hidden = xmlIgnore || ignoreDataMember;

                if (!hidden)
                {
                    var currentValue = propertyInfo.GetValue(obj);
                    expando.Add(propertyInfo.Name, currentValue);
                }
            }
            return expando as ExpandoObject;
        }
    }
}
