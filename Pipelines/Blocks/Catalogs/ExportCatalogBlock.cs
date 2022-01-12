// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCatalogToOrderCloudBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OrderCloud.SDK;
using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Net;
using System.Threading.Tasks;
using Catalog = Sitecore.Commerce.Plugin.Catalog.Catalog;
using OCCatalog = OrderCloud.SDK.Catalog;
using Microsoft.Extensions.Logging;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using System.Linq;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCatalog pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCatalog)]
    public class ExportCatalogBlock : AsyncPipelineBlock<Catalog, Catalog, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCatalogBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCatalogBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Catalog"/>.</returns>
        public override async Task<Catalog> RunAsync(Catalog catalog, CommercePipelineExecutionContext context)
        {
            Condition.Requires(catalog).IsNotNull($"{Name}: The catalog can not be null");

            var client = context.CommerceContext.GetObject<OrderCloudClient>();
            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            var ocCatalog = await GetOrCreateCatalog(client, catalog, context, exportResult);
            if (ocCatalog == null)
            {
                return null;
            }

            var exportSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>();
            var buyerId = exportSettings.CatalogSettings.FirstOrDefault(c => c.CatalogName == ocCatalog.ID).DefaultBuyerId;
            await CreateCatalogAssignment(client, ocCatalog, buyerId, context, exportResult);

            return catalog;
        }

        protected async Task<OCCatalog> GetOrCreateCatalog(OrderCloudClient client, Catalog catalog, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var catalogId = catalog.Name.ToValidOrderCloudId();
            try
            {
                var ocCatalog = await client.Catalogs.GetAsync(catalogId);
                exportResult.Catalogs.ItemsNotChanged++;

                return ocCatalog;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    try
                    {
                        var ocCatalog = new OCCatalog
                        {
                            ID = catalogId,
                            Active = true,
                            Name = catalog.DisplayName
                        };

                        context.Logger.LogInformation($"Saving catalog; Catalog ID: {ocCatalog.ID}");
                        ocCatalog = await client.Catalogs.SaveAsync(catalogId, ocCatalog);
                        exportResult.Catalogs.ItemsCreated++;

                        return ocCatalog;
                    }
                    catch (Exception e)
                    {
                        exportResult.Catalogs.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                OrderCloudConstants.Errors.CreateCatalogFailed,
                                new object[]
                                {
                                    Name,
                                    catalogId,
                                    e.Message,
                                    e
                                },
                                $"{Name}: Ok| Create catalog '{catalogId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                            context);

                        return null;
                    }
                }
                else
                {
                    exportResult.Catalogs.ItemsErrored++;

                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.GetCatalogFailed,
                            new object[]
                            {
                                Name,
                                catalogId,
                                ex.Message,
                                ex
                            },
                            $"{Name}: Ok| Get catalog '{catalogId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                        context);

                    return null;
                }
            }
        }

        protected async Task CreateCatalogAssignment(OrderCloudClient client, OCCatalog catalog, string buyerId, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                var catalogAssignment = new CatalogAssignment
                {
                    CatalogID = catalog.ID,
                    BuyerID = buyerId,
                    ViewAllCategories = true,
                    ViewAllProducts = true
                };

                exportResult.CatalogAssignments.ItemsProcessed++;

                context.Logger.LogInformation($"Saving catalog assignment; Catalog ID: {catalog.ID}, Buyer ID: {buyerId}");
                await client.Catalogs.SaveAssignmentAsync(catalogAssignment);
                exportResult.CatalogAssignments.ItemsUpdated++;

                return;
            }
            catch (Exception ex)
            {
                exportResult.CatalogAssignments.ItemsErrored++;
                context.Logger.LogError($"Saving catalog assignment failed; Catalog ID: {catalog.ID}\n{ex.Message}\n{ex}");
            }
        }
    }
}