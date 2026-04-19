using MaxRev.Gdal.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OSGeo.GDAL;
using OSGeo.OGR;

using rustles.core.Entities.DTOs;
using rustles.core.Events;
using rustles.core.Shared;
using rustles.core.Systems.GDAL;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Systems.ResolutionQue;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Registrations
{
    public static class AddPositionResolutionPipeline
    {
        public static IServiceCollection AddRustlesPositionResolutionPipeline(
            this IServiceCollection services,
            string? rasterPath = null,
            string? municipalityGpkgPath = null)
        {
            var assetsRoot = Path.Combine(AppContext.BaseDirectory, "Assets");
            rasterPath ??= Path.Combine(assetsRoot, "Municipality_5m_3006.tif");
            municipalityGpkgPath ??= Path.Combine(assetsRoot, "kommun-lan-rike_aktuell.gpkg");

            services.AddSingleton(_ =>
            {
                GdalBase.ConfigureAll();
                var dataset = Gdal.Open(rasterPath, Access.GA_ReadOnly);
                return dataset ?? throw new FileNotFoundException("Could not open municipality raster.", rasterPath);
            });

            services.AddSingleton(_ =>
            {
                GdalBase.ConfigureAll();
                var dataSource = Ogr.Open(municipalityGpkgPath, 0);
                return dataSource ?? throw new FileNotFoundException("Could not open municipality GeoPackage.", municipalityGpkgPath);
            });

            services.AddSingleton<IFactory<BatchRequest>, BatchRequestFactory>();
            services.AddSingleton<IFactory<Municipality>, MunicipalityFactory>();

            services.AddSingleton<ITransposeClientCoordinates, TransposeClientCoordinates>();
            services.AddSingleton<IValidateTransposedCoordinates, ValidateTransposedCoordinates>();
            services.AddSingleton<IGeoRasterAccess, GeoRasterAccess>();
            services.AddSingleton<IGeoOgrAccess, GeoOgrAccess>();
            services.AddSingleton<IResolveMunip, ResolveMunip>();

            services.AddSingleton<Batched>();
            services.AddSingleton<ITileBatcher<TileKey>>(sp => sp.GetRequiredService<Batched>());
            services.AddSingleton<ISignalBatchFull>(sp => sp.GetRequiredService<Batched>());
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<Batched>());

            services.AddSingleton<IBatcher<BatchRequest>, Batcher>();
            services.AddSingleton<IRequestPostion, PositionResolveQue>();

            services.AddSingleton<BatchFlusher>();
            services.AddSingleton<IBatchFlusher>(sp => sp.GetRequiredService<BatchFlusher>());
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BatchFlusher>());

            services.AddSingleton<TileWorkResolver>();
            services.AddSingleton<ITileWorkResolver>(sp => sp.GetRequiredService<TileWorkResolver>());
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TileWorkResolver>());

            services.AddSingleton<CompletionSink>();
            services.AddSingleton<ICompletionsink>(sp => sp.GetRequiredService<CompletionSink>());
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CompletionSink>());

            return services;
        }

        private sealed class BatchRequestFactory : IFactory<BatchRequest>
        {
            public BatchRequest Create(params object[]? init)
            {
                if (init is { Length: >= 2 } && init[0] is Guid requestId && init[1] is ClientCell cell)
                    return new BatchRequest(requestId, cell);

                return new BatchRequest(Guid.Empty, new ClientCell(0, 0));
            }
        }

        private sealed class MunicipalityFactory : IFactory<Municipality>
        {
            public Municipality Create(params object[]? init)
            {
                return new Municipality(
                    init?.ElementAtOrDefault(0) as string,
                    ToNullableInt(init?.ElementAtOrDefault(1)),
                    ToNullableInt(init?.ElementAtOrDefault(2)),
                    ToNullableInt(init?.ElementAtOrDefault(3)));
            }

            private static int? ToNullableInt(object? value)
            {
                return value switch
                {
                    int number => number,
                    null => null,
                    _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null
                };
            }
        }
    }
}
