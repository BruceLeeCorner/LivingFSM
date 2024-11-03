using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LivingObject
{
    public interface ICache : IModular
    {
        
    }

    public class Cache : ICache
    {
        private readonly Dictionary<string, object> keyValuePairs;
        public Cache()
        {
            keyValuePairs = new Dictionary<string, object>();

            JsonSerializer.Deserialize<dynamic>("");

            GetCacheValue<dynamic>(() => true);
        }

        public T GetCacheValue<T>(MemberExpression ex)
        {
            return (T)keyValuePairs[""];
        }



        public void SetCacheValue(string name,object value)
        {
            if(keyValuePairs.ContainsKey(name))
            {

            }
            keyValuePairs[name] = value;
        }

        public string Module => throw new NotImplementedException();

        public string Group => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string Id => throw new NotImplementedException();
    }
}
