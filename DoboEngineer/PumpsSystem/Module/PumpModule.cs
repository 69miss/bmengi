using Dobo.Appl.Module;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace PumpsSystem.Module
{
    internal class PumpModule:IModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IJsonTypeInfoResolver, SourceGenerationContext>(p => SourceGenerationContext.Default);
            services.AddSingleton(p =>
            {
                var arr = p.GetServices<IJsonTypeInfoResolver>().ToArray();
                return new JsonSerializerOptions()
                {
                    TypeInfoResolver = JsonTypeInfoResolver.Combine(arr)
                };
            });
            services.AddSingleton<LangBase,PumpLang>();
        }

        public void OnStartup(IServiceProvider serviceProvider)
        {
            Default = serviceProvider;
        }

        public static IServiceProvider Default { get; private set; }
    }
}
