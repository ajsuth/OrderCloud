// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateCategoryBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;
using Sitecore.Commerce.Plugin.Catalog;
using Ajsuth.Sample.OrderCloud.Engine.Models;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ValidateCategory pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ValidateCustomer)]
    public class ValidateCategoryBlock : AsyncPipelineBlock<ExportEntitiesArgument, Category, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ValidateCategoryBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ValidateCategoryBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Category"/>.</returns>
        public override async Task<Category> RunAsync(ExportEntitiesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument cannot be null.");
            Condition.Requires(arg.EntityId).IsNotNull($"{Name}: The entity id cannot be null.");

            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            var category = await Commander.Pipeline<FindEntityPipeline>().RunAsync(new FindEntityArgument(typeof(Customer), arg.EntityId), context).ConfigureAwait(false) as Category;
            if (category == null)
            {
                exportResult.Categories.ItemsErrored++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "CategoryNotFound",
                        new object[] { category.Id },
                        $"Ok| Sellable Item {category.Id} was not found.").ConfigureAwait(false),
                    context);

                return null;
            }

            context.CommerceContext.AddUniqueObjectByType(arg);

            context.Logger.LogDebug($"{Name}: Validating category '{category.Id}'");

            if (!category.Published)
            {
                exportResult.Categories.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "CategoryNotPublished",
                        new object[] { category.Id },
                        $"Ok| Category {category.Id} has not been published and will not be migrated.").ConfigureAwait(false),
                    context);

                return null;
            }

            if (category.HasComponent<PurgeCategoriesComponent>())
            {
                exportResult.Categories.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "CategoryPendingPurge",
                        new object[] { category.Id },
                        $"Ok| Category {category.Id} is pending system deletion and will not be migrated.").ConfigureAwait(false),
                    context);

                return null;
            }

            return category;
        }
    }
}