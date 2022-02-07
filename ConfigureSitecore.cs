// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureSitecore.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;
using Sitecore.Framework.Rules;
using System.Reflection;

namespace Ajsuth.Sample.OrderCloud.Engine
{
    /// <summary>The configure sitecore class.</summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>The configure services.</summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.RegisterAllPipelineBlocks(assembly);
            services.RegisterAllCommands(assembly);

            services.Sitecore().Rules(config => config.Registry(registry => registry.RegisterAssembly(assembly)));

            services.Sitecore().Pipelines(builder => builder

                .ConfigurePipeline<IConfigureOpsServiceApiPipeline>(pipeline => pipeline
                    .Add<Pipelines.Blocks.ConfigureOpsServiceApiBlock>()
                )

                .AddPipeline<IExportBuyersPipeline, ExportBuyersPipeline>(pipeline => pipeline
                    .Add<ValidateDomainBlock>()
                    .Add<ExportBuyerBlock>()
                )

                .AddPipeline<IExportBuyersExtendedPipeline, ExportBuyersExtendedPipeline>(pipeline => pipeline
                    .Add<ValidateStorefrontBlock>()
                    .Add<ExportBuyerExtendedBlock>()
                )

                .AddPipeline<IExportCustomersPipeline, ExportCustomersPipeline>(pipeline => pipeline
                    .Add<ValidateCustomerBlock>()
                    .Add<ExportCustomerBlock>()
                )

                .AddPipeline<IExportCatalogsPipeline, ExportCatalogsPipeline>(pipeline => pipeline
                    .Add<ValidateCatalogBlock>()
                    .Add<ExportCatalogBlock>()
                )

                .AddPipeline<IExportCatalogAssignmentsPipeline, ExportCatalogAssignmentsPipeline>(pipeline => pipeline
                    .Add<ValidateCatalogBlock>()
                    .Add<ExportCatalogAssignmentsBlock>()
                )

                .AddPipeline<IExportCategoriesPipeline, ExportCategoriesPipeline>(pipeline => pipeline
                    .Add<ValidateCategoryBlock>()
                    .Add<ExportCategoryBlock>()
                )

                .AddPipeline<IExportCategoryAssignmentsPipeline, ExportCategoryAssignmentsPipeline>(pipeline => pipeline
                    .Add<ValidateCategoryBlock>()
                    .Add<ExportCategoryAssignmentsBlock>()
                )

                .AddPipeline<IExportSellableItemsPipeline, ExportSellableItemsPipeline>(pipeline => pipeline
                    .Add<ValidateSellableItemBlock>()
                    .Add<ExportSellableItemBlock>()
                )

                .AddPipeline<IExportToOrderCloudPipeline, ExportToOrderCloudPipeline>(pipeline => pipeline
                    .Add<PrepareExportBlock>()
                    .Add<PrepareOrderCloudClientBlock>()
                    .Add<ExportBuyersBlock>()
                    .Add<ExportBuyersExtendedBlock>()
                    .Add<ExportCustomersBlock>()
                    .Add<ExportCatalogsBlock>()
                    .Add<ExportCategoriesBlock>()
                    .Add<ExportSellableItemsBlock>()
                    .Add<ExportAllCategoryAssignmentsBlock>()
                    .Add<ExportAllCatalogAssignmentsBlock>()
                )

            );
        }
    }
}
