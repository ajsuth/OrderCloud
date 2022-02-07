// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCatalogToOrderCloudBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Microsoft.Extensions.Logging;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalog = Sitecore.Commerce.Plugin.Catalog.Catalog;
using OCCatalog = OrderCloud.SDK.Catalog;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCatalog pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCatalog)]
    public class ExportCatalogBlock : AsyncPipelineBlock<Catalog, Catalog, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commerce commander.</summary>
        protected CommerceCommander Commander { get; set; }

        /// <summary>The OrderCloud client.</summary>
        protected OrderCloudClient Client { get; set; }

        /// <summary>The export result model.</summary>
        protected ExportResult Result { get; set; }

        /// <summary>The site settings.</summary>
        protected SitePolicy SiteSettings { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCatalogBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCatalogBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="catalog">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Catalog"/>.</returns>
        public override async Task<Catalog> RunAsync(Catalog catalog, CommercePipelineExecutionContext context)
        {
            Condition.Requires(catalog).IsNotNull($"{Name}: The catalog can not be null");

            Client = context.CommerceContext.GetObject<OrderCloudClient>();
            Result = context.CommerceContext.GetObject<ExportResult>();

            var exportSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>();
            SiteSettings = exportSettings.SiteSettings.FirstOrDefault(site => site.Catalog.EqualsOrdinalIgnoreCase(catalog.FriendlyId));

            var ocCatalog = await GetOrCreateCatalog(context, catalog);
            if (ocCatalog == null)
            {
                return null;
            }

            await CreateOrUpdateCatalogAssignment(context, ocCatalog);
            
            return catalog;
        }

        /// <summary>
        /// Gets or creates a catalog.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="catalog">The XC catalog.</param>
        /// <returns>The <see cref="OrderCloud.SDK.Catalog"/>.</returns>
        protected async Task<OCCatalog> GetOrCreateCatalog(CommercePipelineExecutionContext context, Catalog catalog)
        {
            var catalogId = catalog.FriendlyId.ToValidOrderCloudId();
            try
            {
                var ocCatalog = await Client.Catalogs.GetAsync(catalogId);
                Result.Catalogs.ItemsNotChanged++;

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

                        Result.Catalogs.ItemsProcessed++;

                        context.Logger.LogInformation($"Saving catalog; Catalog ID: {ocCatalog.ID}");
                        ocCatalog = await Client.Catalogs.SaveAsync(catalogId, ocCatalog);
                        Result.Catalogs.ItemsCreated++;

                        return ocCatalog;
                    }
                    catch (Exception e)
                    {
                        Result.Catalogs.ItemsErrored++;

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
                    Result.Catalogs.ItemsErrored++;

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

        /// <summary>
        /// Creates or updates a catalog assignment.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="catalog">The OC catalog.</param>
        protected async Task CreateOrUpdateCatalogAssignment(CommercePipelineExecutionContext context, OCCatalog catalog)
        {
            try
            {
                var catalogAssignment = new CatalogAssignment
                {
                    CatalogID = catalog.ID,
                    BuyerID = SiteSettings.Domain.ToValidOrderCloudId(),
                    ViewAllCategories = true,
                    ViewAllProducts = true
                };

                Result.CatalogAssignments.ItemsProcessed++;

                context.Logger.LogInformation($"Saving catalog assignment; Catalog ID: {catalog.ID}, Buyer ID: {SiteSettings.Domain.ToValidOrderCloudId()}");
                await Client.Catalogs.SaveAssignmentAsync(catalogAssignment);
                Result.CatalogAssignments.ItemsUpdated++;
            }
            catch (Exception ex)
            {
                Result.CatalogAssignments.ItemsErrored++;
                context.Logger.LogError($"Saving catalog assignment failed; Catalog ID: {catalog.ID}, Buyer ID: {SiteSettings.Domain.ToValidOrderCloudId()}\n{ex.Message}\n{ex}");
            }
        }
    }
}