using Dobo.Appl.Dao;
using Dobo.Appl.Entity;
using Dobo.Appl.Module;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Dobo.Appl.Service
{
    public class DataDictSvc:ServiceBase<DataDict>
    {
        JsonSerializerOptions options = ApplModule.Default.GetService<JsonSerializerOptions>();
        //    new()
        //{
        //    TypeInfoResolver = SourceGenerationContext.Default,// new DefaultJsonTypeInfoResolver(),
        //    PropertyNameCaseInsensitive = true,
        //    //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        //};
        public DataDict GetByName(string name) {
            return DbUtility.LiteDb.Select<DataDict>().Where(p=>p.Name==name).First();
        }
        public int DeleteByParentId(long parentId)
        {
            return DbUtility.LiteDb.Select<DataDict>().Where(p => p.ParentId == parentId).ToDelete().ExecuteAffrows();
        }
        public DataDict GetTreeByName(string name)
        {
            return DbUtility.LiteDb.Select<DataDict>().Where(p => p.Name == name).AsTreeCte().ToTreeList().FirstOrDefault();
        }
        public void SetJson<T>(string name, T value)
        {

            var json = JsonSerializer.Serialize<T>(value, options);
            var old = GetByName(name);
            old = SaveToJson(name, json, old);
        }

        private DataDict SaveToJson(string name, string json, DataDict old)
        {
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

            return old;
        }

        public DataDict SetJson<T>(T value, long? id = null, long? parentId=null)
        {
            var json = JsonSerializer.Serialize<T>(value, options);
            if (id != null)
            {

                var old = GetById(id);
                old.Value = json;
                old.UpdateTime = DateTime.Now;
                Update(old);
                return old;
            }
            else
            {
                var old = new DataDict();

                old.Value = json;
                old.CreateTime = DateTime.Now;
                old.UpdateTime = old.CreateTime;
                old.ParentId = parentId;
                old.Id=Add(old);

                return old;
            }

        }
       
        public T? GetByJson<T>(string name) {
            var dict=GetByName(name);
            if (dict == null)
                return default(T);
            return JsonSerializer.Deserialize<T>(dict.Value, options);
        }
        public T? GetByJson<T>(DataDict dict)
        {
            if (dict == null)
                return default(T);
            return JsonSerializer.Deserialize<T>(dict.Value, options);
        }

    }
}
