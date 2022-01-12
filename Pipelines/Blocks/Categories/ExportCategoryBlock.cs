// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCategoryBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OrderCloud.SDK;
using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Category = Sitecore.Commerce.Plugin.Catalog.Category;
using OCCategory = OrderCloud.SDK.Category;
using Ajsuth.Sample.OrderCloud.Engine.Models;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCategory pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCategory)]
    public class ExportCategoryBlock : AsyncPipelineBlock<Category, Category, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCategoryBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCategoryBlock(CommerceCommander commander)
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

            var ocCategory = await GetOrCreateCategory(client, category, context, exportResult);
            if (ocCategory == null)
            {
                return null;
            }

            return category;
        }

        protected async Task<OCCategory> GetOrCreateCategory(OrderCloudClient client, Category category, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var friendlyIdParts = category.FriendlyId.Split("-");
            var catalogId = friendlyIdParts[0].ToValidOrderCloudId();
            var categoryId = friendlyIdParts[1].ToValidOrderCloudId();

            try
            {
                var ocCategory = await client.Categories.GetAsync(catalogId, categoryId);
                exportResult.Categories.ItemsNotChanged++;

                return ocCategory;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    try
                    {
                        var ocCategory = new OCCategory
                        {
                            ID = categoryId,
                            Active = true,
                            Name = category.DisplayName,
                            Description = category.Description
                        };

                        // Cannot set the category ParentID at this time as the parent category may not be created.

                        context.Logger.LogInformation($"Saving category; Catalog ID: {catalogId}, Category ID: {categoryId}");
                        ocCategory = await client.Categories.SaveAsync(catalogId, categoryId, ocCategory);
                        exportResult.Categories.ItemsCreated++;

                        return ocCategory;
                    }
                    catch (Exception e)
                    {
                        exportResult.Categories.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                OrderCloudConstants.Errors.CreateCategoryFailed,
                                new object[]
                                {
                                    Name,
                                    categoryId,
                                    e.Message,
                                    e
                                },
                                $"{Name}: Ok| Create product '{category.FriendlyId}' failed.\n{e.Message}\n{e}").ConfigureAwait(false),
                            context);

                        return null;
                    }
                }
                else
                {
                    exportResult.Categories.ItemsErrored++;

                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.GetCategoryFailed,
                            new object[]
                            {
                                Name,
                                categoryId,
                                ex.Message,
                                ex
                            },
                            $"{Name}: Ok| Get category '{category.FriendlyId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                        context);

                    return null;
                }
            }
        }
    }
}