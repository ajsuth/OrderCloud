// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCatalogAssignmentsBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OrderCloud.SDK;
using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Catalog = Sitecore.Commerce.Plugin.Catalog.Catalog;
using Sitecore.Commerce.Plugin.Catalog;
using System.Linq;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using System.Net;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCatalogAssignments pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCatalogAssignments)]
    public class ExportCatalogAssignmentsBlock : AsyncPipelineBlock<Catalog, Catalog, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCatalogAssignmentsBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCatalogAssignmentsBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Catalog"/>.</returns>
        public override async Task<Catalog> RunAsync(Catalog Catalog, CommercePipelineExecutionContext context)
        {
            Condition.Requires(Catalog).IsNotNull($"{Name}: The customer can not be null");

            var client = context.CommerceContext.GetObject<OrderCloudClient>();
            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            await CreateCatalogProductAssignments(client, Catalog, context, exportResult);

            return Catalog;
        }

        protected async Task CreateCatalogProductAssignments(OrderCloudClient client, Catalog catalog, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var catalogId = catalog.FriendlyId.ToValidOrderCloudId();

            var catalogDependencies = await Commander.Pipeline<IFindEntitiesInListPipeline>()
                 .RunAsync(new FindEntitiesInListArgument(typeof(Catalog),
                         $"{CatalogConstants.CatalogToSellableItem}-{catalog.Id.SimplifyEntityName()}",
                         0,
                         int.MaxValue, false, true),
                     context.CommerceContext.PipelineContextOptions)
                 .ConfigureAwait(false);

            if (!catalogDependencies.EntityReferences.Any())
            {
                return;
            }

            var problemObjects = context.CommerceContext.GetObject<ProblemObjects>();

            foreach (var reference in catalogDependencies.EntityReferences)
            {
                var productId = reference.EntityId.RemoveIdPrefix<SellableItem>().ToValidOrderCloudId();

                exportResult.CatalogProductAssignments.ItemsProcessed++;

                // TODO: Validate if the product has previously errored or been skipped to avoid invalid calls to OrderCloud
                if (problemObjects.Products.Contains(productId))
                {
                    context.Logger.LogInformation($"Skipping Catalog product assignment as product is in problem list; Catalog ID: {catalogId}, Catalog ID: {catalogId}, Product ID: {productId}");
                    exportResult.CatalogAssignments.ItemsSkipped++;
                    continue;
                }

                try
                {
                    var catalogProductAssignment = new ProductCatalogAssignment
                    {
                        CatalogID = catalogId,
                        ProductID = productId
                    };

                    context.Logger.LogInformation($"Saving catalog product assignment; Catalog ID: {catalogId}, Product ID: {productId}");
                    await client.Catalogs.SaveProductAssignmentAsync(catalogProductAssignment);
                    exportResult.CatalogProductAssignments.ItemsUpdated++;
                }
                catch (OrderCloudException ex)
                {
                    if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                    {
                        exportResult.CatalogProductAssignments.ItemsErrored++;

                        context.Logger.LogError($"Error saving catalog product assignment. One or more objects not found; Catalog ID: {catalogId}, Product ID: {productId}");

                        return;
                    }
                    else
                    {
                        exportResult.CatalogProductAssignments.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                OrderCloudConstants.Errors.CreateCatalogProductAssignmentFailed,
                                new object[]
                                {
                                    Name,
                                    catalogId,
                                    ex.Message,
                                    ex
                                },
                                $"{Name}: Ok| Create catalog product assignment '{catalog.FriendlyId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                            context);

                        return;
                    }
                }
            }
        }
    }
}