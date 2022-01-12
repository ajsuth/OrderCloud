// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrepareExportBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Shops;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing PrepareExport pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.PrepareExport)]
    public class PrepareExportBlock : AsyncPipelineBlock<ExportToOrderCloudArgument, ExportToOrderCloudArgument, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="PrepareExportBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public PrepareExportBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Catalog"/>.</returns>
        public override async Task<ExportToOrderCloudArgument> RunAsync(ExportToOrderCloudArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            // Shop probably not needed
            var shop = await Commander.Command<GetShopCommand>().Process(context.CommerceContext, context.CommerceContext.CurrentShopName());

            if (shop == null)
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        OrderCloudConstants.Errors.ShopNotFound,
                        new object[]
                        {
                            Name,
                            context.CommerceContext.CurrentShopName()
                        },
                        $"{Name}: Shop '{context.CommerceContext.CurrentShopName()}' not found.").ConfigureAwait(false),
                    context);
            }

            context.CommerceContext.AddUniqueEntityByType(shop);

            context.CommerceContext.AddObject(new ExportResult());

            return arg;
        }
    }
}