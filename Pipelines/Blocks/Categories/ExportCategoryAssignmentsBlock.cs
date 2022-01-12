// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCategoryAssignmentsBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OrderCloud.SDK;
using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Category = Sitecore.Commerce.Plugin.Catalog.Category;
using Sitecore.Commerce.Plugin.Catalog;
using System.Linq;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using System.Net;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCategoryAssignments pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCategoryAssignments)]
    public class ExportCategoryAssignmentsBlock : AsyncPipelineBlock<Category, Category, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCategoryAssignmentsBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCategoryAssignmentsBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Category"/>.</returns>
        public override async Task<Category> RunAsync(Category category, CommercePipelineExecutionContext context)
        {
            Condition.Requires(category).IsNotNull($"{Name}: The customer can not be null");

            var client = context.CommerceContext.GetObject<OrderCloudClient>();
            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            await UpdateCategoriesWithParentId(client, category, context, exportResult);

            await CreateCategoryProductAssignments(client, category, context, exportResult);

            return category;
        }

        protected async Task UpdateCategoriesWithParentId(OrderCloudClient client, Category category, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var friendlyIdParts = category.FriendlyId.Split("-");
            var catalogId = friendlyIdParts[0].ToValidOrderCloudId();
            var parentCategoryId = friendlyIdParts[1].ToValidOrderCloudId();

            var categoryDependencies = await Commander.Pipeline<IFindEntitiesInListPipeline>()
                 .RunAsync(new FindEntitiesInListArgument(typeof(Category),
                         $"{CatalogConstants.CategoryToCategory}-{category.Id.SimplifyEntityName()}",
                         0,
                         int.MaxValue, false, true),
                     context.CommerceContext.PipelineContextOptions)
                 .ConfigureAwait(false);

            if (!categoryDependencies.EntityReferences.Any())
            {
                return;
            }

            foreach (var reference in categoryDependencies.EntityReferences)
            {
                var categoryId = reference.EntityId.CategoryNameFromCategoryId().ToValidOrderCloudId();

                try
                {
                    var ocCategory = new PartialCategory
                    {
                        ID = categoryId,
                        ParentID = parentCategoryId
                    };

                    exportResult.CategoryAssignments.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving category; Catalog ID: {catalogId}, Category ID: {categoryId}");
                    await client.Categories.PatchAsync(catalogId, categoryId, ocCategory);
                    exportResult.CategoryAssignments.ItemsPatched++;
                }
                catch (Exception ex)
                {
                    exportResult.CategoryAssignments.ItemsErrored++;

                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.UpdateCategoryParentIdFailed,
                            new object[]
                            {
                                    Name,
                                    categoryId,
                                    ex.Message,
                                    ex
                            },
                            $"{Name}: Ok| Create category assignment '{category.FriendlyId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                        context);

                    return;
                }
            }
        }

        protected async Task CreateCategoryProductAssignments(OrderCloudClient client, Category category, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var friendlyIdParts = category.FriendlyId.Split("-");
            var catalogId = friendlyIdParts[0].ToValidOrderCloudId();
            var categoryId = friendlyIdParts[1].ToValidOrderCloudId();

            var categoryDependencies = await Commander.Pipeline<IFindEntitiesInListPipeline>()
                 .RunAsync(new FindEntitiesInListArgument(typeof(Category),
                         $"{CatalogConstants.CategoryToSellableItem}-{category.Id.SimplifyEntityName()}",
                         0,
                         int.MaxValue, false, true),
                     context.CommerceContext.PipelineContextOptions)
                 .ConfigureAwait(false);

            if (!categoryDependencies.EntityReferences.Any())
            {
                return;
            }

            var problemObjects = context.CommerceContext.GetObject<ProblemObjects>();

            foreach (var reference in categoryDependencies.EntityReferences)
            {
                var productId = reference.EntityId.RemoveIdPrefix<SellableItem>().ToValidOrderCloudId();

                exportResult.CategoryProductAssignments.ItemsProcessed++;

                // TODO: Validate if the product has previously errored or been skipped to avoid invalid calls to OrderCloud
                if (problemObjects.Products.Contains(productId))
                {
                    context.Logger.LogInformation($"Skipping category product assignment as product is in problem list; Catalog ID: {catalogId}, Category ID: {categoryId}, Product ID: {productId}");
                    exportResult.CategoryProductAssignments.ItemsSkipped++;
                    continue;
                }

                try
                {
                    var categoryProductAssignment = new CategoryProductAssignment
                    {
                        CategoryID = categoryId,
                        ProductID = productId
                    };

                    context.Logger.LogInformation($"Saving category product assignment; Catalog ID: {catalogId}, Category ID: {categoryId}, Product ID: {productId}");
                    await client.Categories.SaveProductAssignmentAsync(catalogId, categoryProductAssignment);
                    exportResult.CategoryProductAssignments.ItemsUpdated++;
                }
                catch (OrderCloudException ex)
                {
                    if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                    {
                        exportResult.CategoryProductAssignments.ItemsErrored++;

                        context.Logger.LogError($"Error saving category product assignment. One or more objects not found; Catalog ID: {catalogId}, Category ID: {categoryId}, Product ID: {productId}");

                        return;
                    }
                    else
                    {
                        exportResult.CategoryProductAssignments.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                OrderCloudConstants.Errors.CreateCategoryProductAssignmentFailed,
                                new object[]
                                {
                                    Name,
                                    categoryId,
                                    ex.Message,
                                    ex
                                },
                                $"{Name}: Ok| Create category product assignment '{category.FriendlyId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                            context);

                        return;
                    }
                }
            }
        }
    }
}