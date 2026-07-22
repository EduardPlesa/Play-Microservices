namespace Play.Common.MongoDB
{
    public static class MongoExtensions
    {
        public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceSettings = configuration
                .GetSection(nameof(ServiceSettings))
                .Get<ServiceSettings>()
                ?? throw new InvalidOperationException("ServiceSettings configuration is missing.");

            var mongoDbSettings = configuration
                .GetSection(nameof(MongoDbSettings))
                .Get<MongoDbSettings>()
                ?? throw new InvalidOperationException("MongoDbSettings configuration is missing.");

            // The configuration binder sets properties by reflection and does not enforce
            // 'required', so unset values arrive here as null/0 and would otherwise only
            // fail much later inside the driver.
            if (string.IsNullOrWhiteSpace(serviceSettings.ServiceName))
            {
                throw new InvalidOperationException($"{nameof(ServiceSettings)}:{nameof(ServiceSettings.ServiceName)} is not configured.");
            }

            if (string.IsNullOrWhiteSpace(mongoDbSettings.Host))
            {
                throw new InvalidOperationException($"{nameof(MongoDbSettings)}:{nameof(MongoDbSettings.Host)} is not configured.");
            }

            if (mongoDbSettings.Port <= 0)
            {
                throw new InvalidOperationException($"{nameof(MongoDbSettings)}:{nameof(MongoDbSettings.Port)} is not configured.");
            }

            services.AddSingleton(serviceSettings);
            services.AddSingleton(mongoDbSettings);
            services.AddSingleton<IMongoClient>(
                new MongoClient(mongoDbSettings.ConnectionString));
            services.AddSingleton(serviceProvider =>
            {
                var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
                return mongoClient.GetDatabase(serviceSettings.ServiceName);
            });

            return services;
        }

        public static IServiceCollection AddMongoRepository<T>(this IServiceCollection services, string collectionName) where T : IEntity
        {
            services.AddSingleton<IRepository<T>>(serviceProvider =>
            {
                var database = serviceProvider.GetRequiredService<IMongoDatabase>();
                return new MongoRepository<T>(database, collectionName);
            });

            return services;
        }
    }
}
