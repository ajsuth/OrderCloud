// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateDomain.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;
using Sitecore.Commerce.Plugin.Shops;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ValidateDomain pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ValidateDomain)]
    public class ValidateDomainBlock : AsyncPipelineBlock<ExportEntitiesArgument, Shop, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ValidateDomainBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ValidateDomainBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Shop"/>.</returns>
        public override async Task<Shop> RunAsync(ExportEntitiesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument cannot be null.");
            Condition.Requires(arg.EntityId).IsNotNull($"{Name}: The domain id cannot be null.");

            var storefront = await Commander.Command<GetShopCommand>().Process(context.CommerceContext, arg.EntityId);

            if (storefront == null)
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        OrderCloudConstants.Errors.StorefrontNotFound,
                        new object[]
                        {
                            Name,
                            arg.EntityId
                        },
                        $"{Name}: Storefront '{arg.EntityId}' not found.").ConfigureAwait(false),
                    context);
            }

            context.CommerceContext.AddUniqueObjectByType(arg);
            context.CommerceContext.AddUniqueEntity(storefront);

            context.Logger.LogDebug($"{Name}: Validating domain '{storefront.Id}'");

            return storefront;
        }
    }
}