// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrepareOrderCloudClientBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing PrepareOrderCloudClient pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCatalogs)]
    public class PrepareOrderCloudClientBlock : AsyncPipelineBlock<ExportToOrderCloudArgument, ExportToOrderCloudArgument, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="PrepareOrderCloudClientBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public PrepareOrderCloudClientBlock(CommerceCommander commander)
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

            var orderCloudPolicy = context.GetPolicy<OrderCloudClientPolicy>();
            if (!orderCloudPolicy.IsValid())
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.CommerceContext.GetPolicy<KnownResultCodes>().Error,
                        OrderCloudConstants.Errors.InvalidOrderCloudClientPolicy,
                        null,
                        "Invalid OrderCloud Client Policy").ConfigureAwait(false),
                    context);

                return null;
            }

            try
            {
                var client = new OrderCloudClient(new OrderCloudClientConfig
                {
                    ClientId = orderCloudPolicy.ClientId,
                    ClientSecret = orderCloudPolicy.ClientSecret,
                    Roles = new[] { ApiRole.FullAccess },
                    ApiUrl = orderCloudPolicy.ApiUrl,
                    AuthUrl = orderCloudPolicy.AuthUrl
                });

                context.CommerceContext.AddObject(client);

                return arg;
            }
            catch(Exception ex)
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.CommerceContext.GetPolicy<KnownResultCodes>().Error,
                        OrderCloudConstants.Errors.InvalidOrderCloudClientPolicy,
                        new object[]
                        {
                            Name,
                            ex.Message,
                            ex
                        },
                        $"{Name}: Ok| Create OrderCloud client failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                    context);

                return null;
            }
        }
    }
}