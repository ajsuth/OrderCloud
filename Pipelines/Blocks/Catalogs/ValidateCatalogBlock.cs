// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateCatalogBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using System.Linq;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ValidateCatalog pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ValidateCatalog)]
    public class ValidateCatalogBlock : AsyncPipelineBlock<ExportEntitiesArgument, Catalog, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ValidateCatalogBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ValidateCatalogBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Catalog"/>.</returns>
        public override async Task<Catalog> RunAsync(ExportEntitiesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument cannot be null.");
            Condition.Requires(arg.EntityId).IsNotNull($"{Name}: The entity id cannot be null.");

            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            var catalog = await Commander.Pipeline<FindEntityPipeline>().RunAsync(new FindEntityArgument(typeof(Catalog), arg.EntityId), context).ConfigureAwait(false) as Catalog;
            if (catalog == null)
            {
                exportResult.Catalogs.ItemsErrored++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "CatalogNotFound",
                        new object[] { arg.EntityId },
                        $"Catalog {arg.EntityId} was not found.").ConfigureAwait(false),
                    context);
                return null;
            }

            context.CommerceContext.AddUniqueObjectByType(arg);

            context.Logger.LogDebug($"{Name}: Validating catalog '{catalog.Id}'");

            if (!arg.SiteSettings.Any(c => c.Catalog.EqualsOrdinalIgnoreCase(catalog.Name)))
            {
                exportResult.Catalogs.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "CatalogNotSelected",
                        new object[] { catalog.Id },
                        $"Ok| Catalog {catalog.Id} has not been selected for migration.").ConfigureAwait(false),
                    context);

                return null;
            }

            if (!catalog.Published)
            {
                exportResult.Catalogs.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "CatalogNotPublished",
                        new object[] { catalog.Id },
                        $"Ok| Catalog {catalog.Id} has not been published and will not be migrated.").ConfigureAwait(false),
                    context);

                return null;
            }

            if (catalog.HasComponent<PurgeCatalogsComponent>())
            {
                exportResult.Catalogs.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "CatalogPendingPurge",
                        new object[] { catalog.Id },
                        $"Ok| Catalog {catalog.Id} is pending system deletion and will not be migrated.").ConfigureAwait(false),
                    context);

                return null;
            }

            return catalog;
        }
    }
}