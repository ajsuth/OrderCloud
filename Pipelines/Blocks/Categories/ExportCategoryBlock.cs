// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCategoryBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Microsoft.Extensions.Logging;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Net;
using System.Threading.Tasks;
using Category = Sitecore.Commerce.Plugin.Catalog.Category;
using OCCategory = OrderCloud.SDK.Category;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCategory pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCategory)]
    public class ExportCategoryBlock : AsyncPipelineBlock<Category, Category, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commerce commander.</summary>
        protected CommerceCommander Commander { get; set; }

        /// <summary>The OrderCloud client.</summary>
        protected OrderCloudClient Client { get; set; }

        /// <summary>The export result model.</summary>
        protected ExportResult Result { get; set; }

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

            Client = context.CommerceContext.GetObject<OrderCloudClient>();
            Result = context.CommerceContext.GetObject<ExportResult>();

            var ocCategory = await GetOrCreateCategory(context, category);
            if (ocCategory == null)
            {
                return null;
            }

            return category;
        }

        protected async Task<OCCategory> GetOrCreateCategory(CommercePipelineExecutionContext context, Category category)
        {
            var friendlyIdParts = category.FriendlyId.Split("-");
            var catalogId = friendlyIdParts[0].ToValidOrderCloudId();
            var categoryId = friendlyIdParts[1].ToValidOrderCloudId();

            try
            {
                var ocCategory = await Client.Categories.GetAsync(catalogId, categoryId);
                Result.Categories.ItemsNotChanged++;

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
                        ocCategory = await Client.Categories.SaveAsync(catalogId, categoryId, ocCategory);
                        Result.Categories.ItemsCreated++;

                        return ocCategory;
                    }
                    catch (Exception e)
                    {
                        Result.Categories.ItemsErrored++;

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
                                $"{Name}: Ok| Create category '{category.FriendlyId}' failed.\n{e.Message}\n{e}").ConfigureAwait(false),
                            context);

                        return null;
                    }
                }
                else
                {
                    Result.Categories.ItemsErrored++;

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