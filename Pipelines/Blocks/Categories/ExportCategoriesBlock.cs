// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCategoriesBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Linq;
using System.Threading.Tasks;
using OrderCloudClient = OrderCloud.SDK.OrderCloudClient;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCategories pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCategories)]
    public class ExportCategoriesBlock : AsyncPipelineBlock<ExportToOrderCloudArgument, ExportToOrderCloudArgument, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCategoriesBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCategoriesBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="ExportToOrderCloudArgument"/>.</returns>
        public override async Task<ExportToOrderCloudArgument> RunAsync(ExportToOrderCloudArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            if (!arg.ProcessSettings.ProcessCategories)
            {
                context.Logger.LogInformation($"Skipping category export - not enabled.");
                return arg;
            }

            long itemsProcessed = 0;

            var listName = OrderCloudConstants.Lists.Categories;

            var items = await GetListIds<Category>(context, listName, int.MaxValue).ConfigureAwait(false);
            var listCount = items.List.TotalItemCount;

            context.Logger.LogInformation($"{Name}-Reviewing List:{listName}|Count:{listCount}|Environment:{context.CommerceContext.Environment.Name}");

            if (listCount == 0)
            {
                return arg;
            }

            itemsProcessed += listCount;

            foreach (var entityId in items.EntityReferences.Select(e => e.EntityId))
            {
                var error = false;

                var newContext = new CommercePipelineExecutionContextOptions(new CommerceContext(context.CommerceContext.Logger, context.CommerceContext.TelemetryClient)
                {
                    Environment = context.CommerceContext.Environment,
                    Headers = context.CommerceContext.Headers,
                },
                onError: x => error = true,
                onAbort: x =>
                {
                    if (!x.Contains("Ok|", StringComparison.OrdinalIgnoreCase))
                    {
                        error = true;
                    }
                });

                newContext.CommerceContext.AddObject(context.CommerceContext.GetObject<OrderCloudClient>());
                newContext.CommerceContext.AddObject(context.CommerceContext.GetObject<ExportResult>());

                context.Logger.LogDebug($"{Name}-Exporting category: '{entityId}'. Environment: {context.CommerceContext.Environment.Name}");
                await Commander.Pipeline<ExportCategoriesPipeline>()
                    .RunAsync(
                        new ExportEntitiesArgument(entityId, arg),
                        newContext)
                    .ConfigureAwait(false);

                if (error)
                {
                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.ExportCategoriesFailed,
                            new object[] { Name },
                            $"{Name}: Export categories failed.").ConfigureAwait(false),
                        context);
                }
            }

            var exportResult = context.CommerceContext.GetObject<ExportResult>();
            exportResult.Categories.ItemsProcessed = itemsProcessed;

            context.Logger.LogInformation($"{Name}-Exporting categories Completed: {(int)itemsProcessed}. Environment: {context.CommerceContext.Environment.Name}");
            return arg;
        }

        protected virtual async Task<FindEntitiesInListArgument> GetListIds<T>(CommercePipelineExecutionContext context, string listName, int take, int skip = 0)
        {
            var arg = new FindEntitiesInListArgument(typeof(T), listName, skip, take)
            {
                LoadEntities = false,
                LoadTotalItemCount = true
            };
            var result = await Commander.Pipeline<FindEntitiesInListPipeline>().RunAsync(arg, context.CommerceContext.PipelineContextOptions).ConfigureAwait(false);

            return result;
        }
    }
}