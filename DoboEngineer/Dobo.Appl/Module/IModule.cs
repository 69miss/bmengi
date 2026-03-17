using Dobo.Appl.Device;
using Dobo.Appl.HunterCmd;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
 
namespace Dobo.Appl.Module;
public interface IModule
{

    void ConfigureServices(IServiceCollection services);

    void OnStartup(IServiceProvider serviceProvider);
}
public class ApplModule: IModule
{


   public void ConfigureServices(IServiceCollection services) {

         services.AddKeyedTransient<IProtocolAdapter>("HTST", (p, key) => new HTClient());
        services.AddSingleton<IJsonTypeInfoResolver, SourceGenerationContext>(p => SourceGenerationContext.Default);
        //Program.Main2(null);
    }

   public void OnStartup(IServiceProvider serviceProvider) {

        Default = serviceProvider;
    }

    private static IServiceProvider service;

    public static IServiceProvider Default { get => service;private set => service = value; }
}
public static class  ApplModuleExt{
    public static IServiceCollection AddApplModule(this IServiceCollection services) {

        services.AddSingleton<IModule, ApplModule>();

        return services;
    
    }
}