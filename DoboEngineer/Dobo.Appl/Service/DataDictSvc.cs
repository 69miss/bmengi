using Dobo.Appl.Dao;
using Dobo.Appl.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dobo.Appl.Service
{
    public class DataDictSvc:ServiceBase<DataDict>
    {
        public DataDict GetByName(string name) {
            return DbUtility.LiteDb.Select<DataDict>().Where(p=>p.Name==name).First();
        }
        public void SetJson(string name, object value)
        {
            var json = JsonSerializer.Serialize(value);
            var old = GetByName(name);
            if (old != null)
            {
                old.Value = json;
                old.UpdateTime = DateTime.Now;
                Update(old);
            }
            else
            {
                old = new DataDict();
                old.Name = name;
                old.Value = json;
                old.CreateTime = DateTime.Now;
                old.UpdateTime = old.CreateTime;
                Add(old);
            }
        }
        public T? GetByJson<T>(string name) {
            var dict=GetByName(name);
            if (dict == null)
                return default(T);
            return JsonSerializer.Deserialize<T>(dict.Value);
        }
    }
}
