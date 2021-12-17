using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DotNetCore.CAP;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataContextContainer
{
    class Program
    {
        public static string ConnectionStringTemplate = "Host=localhost;DataBase={{username}};Username=postgres;Password=pass123";
        async static Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            await new HostBuilder()
       .UseEnvironment("Development")
       .ConfigureLogging((hostContext, configLogging) =>
           {
               configLogging.AddConsole().SetMinimumLevel(LogLevel.Information);
           })
       .ConfigureServices((hostContext, services) =>
            {
                services
                   .AddScoped<ConnectionStringProvider>()
                   .AddScoped<ConnectionStringInterceptor>()
                   .AddScoped<ISchemaNameProvider, SchemaNameProvider>()
                   .AddDbContext<UserContext>((provide, optionsBuilder) =>
                   optionsBuilder.UseNpgsql("Host=localhost;DataBase=shared;Username=postgres;Password=pass123",
                                               r =>
                                               {
                                                   r.MigrationsHistoryTable("__EFMigrationsHistory", provide.GetService<ISchemaNameProvider>().SchemaName);
                                               }
                                              )
                   .ReplaceService<IMigrationsCodeGenerator, SchemaAwareCSharpMigrationsGenerator>()
                   .ReplaceService<IModelCacheKeyFactory, MultiTenantModelCacheKeyFactory>()
                   .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>()
                   )
                  .AddTransient<DummySubscribe>()
                  .AddHostedService<PublishMessageEvery5Seconds>()
                  ;
                services.AddLogging(r => r.AddConsole());
                services.AddCap(builder =>
                {
                    builder.UseEntityFramework<UserContext>(r => r.Schema = "cap");
                    //builder.UsePostgreSql(r => {
                    //    r.ConnectionString = "Host=localhost;DataBase=shared;Username=postgres;Password=pass123";
                    //    r.Schema = "cap";
                    //    });
                    builder.SucceedMessageExpiredAfter = 5 * 60;
                    builder.UseKafka(r =>
                    {
                        r.Servers = "localhost:9092";
                        r.ConnectionPoolSize = 20;

                    });
                });
            })
       .RunConsoleAsync();


            var collection = new ServiceCollection();




            var serviceProvider = collection.BuildServiceProvider();

            //configure console logging
            using var scope = serviceProvider.CreateScope();

            //var dbContext = scope.ServiceProvider.GetService<UserContext>();
            //dbContext.Database.EnsureCreated();
            //var publisher = scope.ServiceProvider.GetService<ICapPublisher>();
            //var i = 0;
            //await Task.Run(async () =>
            // {
            //     while (true)
            //     {
            //         Console.WriteLine("publishing :" + i);
            //         publisher.Publish("product.121", i++.ToString());
            //         publisher.Publish("product.122", i++.ToString());
            //         publisher.Publish("product.123", i++.ToString());
            //         await Task.Delay(3000);
            //     }
            // });

            //






            //var users = dbContext.Users.ToList();

            //  users.ForEach(r => Console.WriteLine(r.UserName));
            var selectedUser = Console.ReadLine();

            InitDatabase(selectedUser);
            Console.ReadLine();


        }

        public static void InitDatabase(string databaseName)
        {
            //using var scope = ServiceProvider.CreateScope();
            ////var cs = ConnectionStringTemplate.Replace("{{username}}", databaseName.Trim());//.Replace("\n", "").Replace("\r", ""));
            ////var csProvider = scope.ServiceProvider.GetService<ConnectionStringProvider>();
            ////csProvider.ConnectionString = cs;
            //var dbContext = scope.ServiceProvider.GetService<UserContext>();
            //// dbContext.Database.SetConnectionString();
            //dbContext.Database.EnsureCreated();
            //Console.WriteLine("Database created : {0}", databaseName);
        }
    }


    public class MyDesignTimeServices : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection services)
        {
            services.AddDbContext<UserContext>((provide, optionsBuilder) =>
                  optionsBuilder.UseNpgsql("Host=localhost;DataBase=shared;Username=postgres;Password=pass123",
                                              r =>
                                                 r.MigrationsHistoryTable("__EFMigrationsHistory", provide.GetService<ISchemaNameProvider>().SchemaName)
                                             ));
            services.AddSingleton<IMigrationsCodeGenerator, SchemaAwareCSharpMigrationsGenerator>();
            services.AddSingleton<ICSharpMigrationOperationGenerator, SchemaAwareCSharpMigrationOperationGenerator>();

            services.AddSingleton<ISchemaNameProvider>(r => new SchemaNameProvider { SchemaName = "user" });
            services.RemoveAll<IMigrationsAssembly>();
            services.AddSingleton<IMigrationsAssembly, DbSchemaAwareDesignMigrationAssembly>();
        }
    }


    public class ConnectionStringProvider
    {
        public string ConnectionString { get; set; }
    }

    public class ConnectionStringInterceptor : DbConnectionInterceptor
    {
        private readonly ConnectionStringProvider connectionStringProvider;

        public ConnectionStringInterceptor(ConnectionStringProvider connectionStringProvider)
        {
            this.connectionStringProvider = connectionStringProvider;
        }

        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            if (this.connectionStringProvider != null && !string.IsNullOrWhiteSpace(this.connectionStringProvider.ConnectionString))
            {
                connection.ConnectionString = this.connectionStringProvider.ConnectionString;
            }
            return base.ConnectionOpening(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {

            if (this.connectionStringProvider != null && !string.IsNullOrWhiteSpace(this.connectionStringProvider.ConnectionString))
            {
                connection.ConnectionString = this.connectionStringProvider.ConnectionString;
            }
            return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
        }
    }

    [CapSubscribe("product")]
    public class ProductSubscriber : ICapSubscribe
    {
        [CapSubscribe("121", isPartial: true)]
        public void Product111(string productName)
        {
            Console.WriteLine("Product received : " + productName);
        }

        [CapSubscribe("112")]
        public void Product112(string productName)
        {
            Console.WriteLine("Product received : " + productName);
        }


        [CapSubscribe("113")]
        public void Product113(string productName)
        {
            Console.WriteLine("Product received : " + productName);
        }
    }


    public class DummySubscribe : ICapSubscribe
    {
        [CapSubscribe("^product.*")]
        public void Product111(object productName)
        {
            Console.WriteLine("Product received dummy all : " + productName);
        }

    }


    public class PublishMessageEvery5Seconds : IHostedService, ICapSubscribe
    {
        private Timer _timer;
        private readonly ICapPublisher _eventBus;
        private readonly ILogger _logger;
        private readonly IServiceProvider serviceProvider;
        private int i = 0;
        public PublishMessageEvery5Seconds(ICapPublisher eventBus,
            ILogger<PublishMessageEvery5Seconds> logger,
            IServiceProvider serviceProvider)
        {
            _eventBus = eventBus;
            _logger = logger;
            this.serviceProvider = serviceProvider;

            using var scope = serviceProvider.CreateScope();
            var databaseName = "user124";
            var schemaNameProvider = scope.ServiceProvider.GetService<ISchemaNameProvider>();
            schemaNameProvider.SchemaName = databaseName;
            var context = scope.ServiceProvider.GetService<UserContext>();
            context.Database.Migrate();

            //var cs = ConnectionStringTemplate.Replace("{{username}}", databaseName.Trim());//.Replace("\n", "").Replace("\r", ""));
            //var csProvider = scope.ServiceProvider.GetService<ConnectionStringProvider>();
            //csProvider.ConnectionString = cs;
            // var dbContext = scope.ServiceProvider.GetService<UserContext>();

            // dbContext.Database.SetConnectionString();
            // 
            //  dbContext.Database.EnsureCreated();

            Console.WriteLine("Database created : {0}", databaseName);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");

            _timer = new Timer(Publish, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void Publish(object state)
        {
            Console.WriteLine("publishing");
            _eventBus.Publish("product.121", i++.ToString());
        }

        // [CapSubscribe("product.121")]
        public void Product113(string productName)
        {
            Console.WriteLine("Product received hosted : " + productName);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }


    public class MultiTenantModelCacheKey : ModelCacheKey
    {
        private readonly string _schemaName;

        public MultiTenantModelCacheKey(string schemaName, DbContext context) : base(context)
        {
            _schemaName = string.IsNullOrWhiteSpace(schemaName) ? context.GetType().Name : schemaName;
        }
        public override int GetHashCode()
        {
            return _schemaName.GetHashCode();
        }
    }

    public class MultiTenantModelCacheKeyFactory : ModelCacheKeyFactory
    {
        private string schemaName;
        public MultiTenantModelCacheKeyFactory(ModelCacheKeyFactoryDependencies dependencies) : base(dependencies)
        {
        }

        public override object Create(DbContext context)
        {
            if (context is IHaveSchemaName schema)
            {
                this.schemaName = schema.SchemaName;
            }

            return new MultiTenantModelCacheKey(schemaName, context);
        }
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
    public class DbSchemaAwareMigrationAssembly : MigrationsAssembly
    {
        private readonly DbContext _context;
        private ILogger logger;
        public DbSchemaAwareMigrationAssembly(ICurrentDbContext currentContext,
              IDbContextOptions options, IMigrationsIdGenerator idGenerator,
              IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
          : base(currentContext, options, idGenerator, logger)
        {
            this.logger = logger.Logger;
            this.logger.LogInformation("----SCHEMA AWARE MIGRATION ASSEMBLY----");
            _context = currentContext.Context;
        }

        public override Migration CreateMigration(TypeInfo migrationClass,
              string activeProvider)
        {
            if (activeProvider == null)
                throw new ArgumentNullException(nameof(activeProvider));

            if (_context is not IHaveSchemaName schema)
            {
                return base.CreateMigration(migrationClass, activeProvider);
            }

            var instance = (Migration)Activator.CreateInstance(migrationClass.AsType());
            

            var isPublicSchema = schema.SchemaName == "public";
            if (instance is IHaveSchemaName schemaContainer)
            {
                if (isPublicSchema)
                {
                    this.logger.LogInformation("Skipping Migration : " + migrationClass.Name);
                    return EmptyMigration.GetNewInstance();
                }

                this.logger.LogInformation($"Current Schema : {schemaContainer.SchemaName} Migration : {migrationClass.Name}");

                schemaContainer.SchemaName = schema.SchemaName;
                instance.ActiveProvider = activeProvider;
                return instance;

            }
            else
            {
                if (isPublicSchema)
                {
                    return instance;
                }

                this.logger.LogInformation("Skipping Migration : " + migrationClass.Name);
                return EmptyMigration.GetNewInstance();
            }
        }      
    }


    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
    public class DbSchemaAwareDesignMigrationAssembly : MigrationsAssembly
    {
        private readonly DbContext context;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        public DbSchemaAwareDesignMigrationAssembly(ICurrentDbContext currentContext,
              IDbContextOptions options, IMigrationsIdGenerator idGenerator,
              IDiagnosticsLogger<DbLoggerCategory.Migrations> logger,
              IServiceProvider serviceProvider)
          : base(currentContext, options, idGenerator, logger)
        {
            this.logger = logger.Logger;
            this.logger.LogInformation("----SCHEMA AWARE DESIGN TIME MIGRATION ASSEMBLY----");
            this.context = currentContext.Context;
            this.serviceProvider = serviceProvider;
        }

        public override Migration CreateMigration(TypeInfo migrationClass,
              string activeProvider)
        {
            if (activeProvider == null)
                throw new ArgumentNullException(nameof(activeProvider));

            if (context is not IHaveSchemaName schema)
            {
                return base.CreateMigration(migrationClass, activeProvider);
            }

            var currentSchemaProvider = serviceProvider.GetService<ISchemaNameProvider>();
            var instance = (Migration)Activator.CreateInstance(migrationClass.AsType());
                        
            var isPublicSchema = currentSchemaProvider.SchemaName == "public";
            if (instance is IHaveSchemaName schemaContainer)
            {
                if (isPublicSchema)
                {
                    this.logger.LogInformation("Skipping Migration : " + migrationClass.Name);
                    return EmptyMigration.Instance;
                }

                this.logger.LogInformation($"Current Schema : {schemaContainer.SchemaName} Migration : {migrationClass.Name}");

                schemaContainer.SchemaName = schema.SchemaName;
                instance.ActiveProvider = activeProvider; 
            }
            return instance; 
        }
    }

}
