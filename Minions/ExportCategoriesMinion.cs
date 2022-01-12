// <copyright file="ExportCategoriesMinion.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Exceptions;
using Sitecore.Commerce.Plugin.Catalog;
using Ajsuth.Sample.OrderCloud.Engine.Policies;

namespace Ajsuth.Sample.OrderCloud.Engine.Minions
{
    /// <summary>
    /// Defines the export Categories minion.
    /// </summary>
    /// <seealso cref="Sitecore.Commerce.Core.Minion" />
    public class ExportCategoriesMinion : Minion
    {
        /// <summary>
        /// Gets or sets the minion pipeline.
        /// </summary>
        /// <value>
        /// The minion pipeline.
        /// </value>
        protected IExportCategoryAssignmentsMinionPipeline MinionPipeline { get; set; }

        /// <summary>
        /// Initializes the specified service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="policy">The policy.</param>
        /// <param name="commerceContext">The commerce context.</param>
        public override void Initialize(IServiceProvider serviceProvider, MinionPolicy policy, CommerceContext commerceContext)
        {
            base.Initialize(serviceProvider, policy, commerceContext);

            MinionPipeline = serviceProvider.GetService<IExportCategoryAssignmentsMinionPipeline>();
        }

        /// <inheritdoc />
        /// <summary>
        /// Starts the minion instance.
        /// </summary>
        /// <returns>
        /// The task that is running the minion.
        /// </returns>
        public override Task StartAsync()
        {
            // do nothing, as we do not want it to run at environment startup
            Logger.LogDebug($"{Name} - Export entity minions do not auto start");
            return null;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="MinionRunResultsModel" />
        /// </returns>
        protected override async Task<MinionRunResultsModel> Execute()
        {
            long itemsProcessed = 0;

            foreach (var listToWatch in Policy.ListsToWatch)
            {
                var items = await GetListIds<Category>(listToWatch, int.MaxValue).ConfigureAwait(false);
                var listCount = items.List.TotalItemCount;

                Logger.LogInformation($"{Name}-Reviewing List:{listToWatch}|Count:{listCount}|Environment:{MinionContext.Environment.Name}");

                if (listCount == 0)
                {
                    continue;
                }

                itemsProcessed += listCount;

                foreach (var entityId in items.EntityReferences.Select(e => e.EntityId))
                {
                    var error = false;

                    var context = new CommercePipelineExecutionContextOptions(new CommerceContext(Logger, MinionContext.TelemetryClient)
                    {
                        Environment = Environment,
                        Headers = MinionContext.Headers,
                    },
                    onError: x => error = true,
                    onAbort: x =>
                    {
                        if (!x.Contains("Ok|", StringComparison.OrdinalIgnoreCase))
                        {
                            error = true;
                        }
                    });

                    Logger.LogDebug($"{Name}-Exporting category: '{entityId}'. Environment: {MinionContext.Environment.Name}");

                    await MinionPipeline
                        .RunAsync(
                            new ExportEntitiesMinionArgument(entityId, listToWatch),
                            context)
                        .ConfigureAwait(false);

                    if (error)
                    {
                        throw new MinionExecutionException($"An unhandled error occured while executing the minion '{GetType().FullName}'.");
                    }
                }
            }

            Logger.LogInformation($"{Name}-Exporting categories Completed: {(int)itemsProcessed}. Environment: {MinionContext.Environment.Name}");
            return new MinionRunResultsModel
            {
                DidRun = true,
                ItemsProcessed = (int)itemsProcessed
            };
        }
    }
}
