// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportBuyerExtendedBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Microsoft.Extensions.Logging;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Shops;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportBuyerExtended pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportBuyerExtended)]
    public class ExportBuyerExtendedBlock : AsyncPipelineBlock<Shop, Shop, CommercePipelineExecutionContext>
    {
        /// <summary>The commerce commander.</summary>
        protected CommerceCommander Commander { get; set; }

        /// <summary>The OrderCloud client.</summary>
        protected OrderCloudClient Client { get; set; }

        /// <summary>The export result model.</summary>
        protected ExportResult Result { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportBuyerExtendedBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportBuyerExtendedBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Shop"/>.</returns>
        public override async Task<Shop> RunAsync(Shop shop, CommercePipelineExecutionContext context)
        {
            Condition.Requires(shop).IsNotNull($"{Name}: The customer can not be null");

            Client = context.CommerceContext.GetObject<OrderCloudClient>();
            Result = context.CommerceContext.GetObject<ExportResult>();

            var exportSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>();
            var siteSettings = exportSettings.SiteSettings.FirstOrDefault(site => site.Storefront.EqualsOrdinalIgnoreCase(shop.Id));
            var buyerId = siteSettings.Domain.ToValidOrderCloudId();

            await CreateOrUpdateBuyerUserGroups(context, buyerId, shop);

            var locales = await CreateOrUpdateLocales(context, shop);
            if (locales != null)
            {
                await CreateOrUpdateLocaleAssignments(context, buyerId, shop, locales);
            }

            return shop;
        }

        /// <summary>
        /// Creates or updates the buyer user groups.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buyerId">The buyer identifier.</param>
        /// <param name="exportResult"></param>
        /// <returns></returns>
        protected async Task<List<UserGroup>> CreateOrUpdateBuyerUserGroups(CommercePipelineExecutionContext context, string buyerId, Shop shop)
        {
            var userGroups = new List<UserGroup>();

            foreach (var currency in shop.Currencies)
            {
                var buyerGroupId = $"{buyerId}_{currency.Code}";
                try
                {
                    var userPolicy = context.GetPolicy<UserPolicy>();
                    var userGroup = new UserGroup
                    {
                        ID = buyerGroupId,
                        Name = buyerGroupId
                    };

                    Result.BuyerGroups.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving buyer user group; User Group ID: {buyerGroupId}");
                    userGroup = await Client.UserGroups.SaveAsync(buyerId, userGroup.ID, userGroup);
                    Result.BuyerGroups.ItemsUpdated++;

                    userGroups.Add(userGroup);
                }
                catch (Exception ex)
                {
                    Result.BuyerGroups.ItemsErrored++;
                    context.Logger.LogError($"{Name}: Saving buyer user group '{buyerGroupId}' failed.\n{ex.Message}\n{ex}");

                    continue;
                }
            }

            return userGroups;
        }

        /// <summary>
        /// Creates or updates the locales.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected async Task<List<Locale>> CreateOrUpdateLocales(CommercePipelineExecutionContext context, Shop shop)
        {
            var locales = new List<Locale>();

            foreach (var currency in shop.Currencies)
            {
                var localeId = $"Locale_{currency}";
                try
                {
                    var userPolicy = context.GetPolicy<UserPolicy>();
                    var locale = new Locale
                    {
                        ID = localeId,
                        Currency = currency.Code
                    };

                    Result.Locales.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving locale; Locale ID: {localeId}");
                    locale = await Client.Locales.SaveAsync(localeId, locale);
                    Result.Locales.ItemsUpdated++;

                    locales.Add(locale);
                }
                catch (Exception ex)
                {
                    Result.Locales.ItemsErrored++;
                    context.Logger.LogError($"{Name}: Saving locale '{localeId}' failed.\n{ex.Message}\n{ex}");

                    continue;
                }
            }

            return locales;
        }

        /// <summary>
        /// Creates locale assignments to support multi-currency.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="product">The OC product.</param>
        /// <param name="priceSchedules">The OC price schedules created for the product.</param>
        /// <returns></returns>
        protected async Task CreateOrUpdateLocaleAssignments(CommercePipelineExecutionContext context, string buyerId, Shop shop, List<Locale> locales)
        {
            foreach (var locale in locales)
            {
                foreach (var currency in shop.Currencies)
                {
                    var userGroupId = $"{buyerId}_{currency.Code}";
                    try
                    {
                        var localeAssignment = new LocaleAssignment
                        {
                            LocaleID = locale.ID,
                            BuyerID = buyerId,
                            UserGroupID = userGroupId
                        };

                        Result.LocaleAssignments.ItemsProcessed++;

                        context.Logger.LogInformation($"Saving locale assignment; Locale ID: {locale.ID}, Buyer ID: {buyerId}, User Group ID: {userGroupId}");
                        await Client.Locales.SaveAssignmentAsync(localeAssignment);
                        Result.LocaleAssignments.ItemsCreated++;
                    }
                    catch (Exception ex)
                    {
                        Result.LocaleAssignments.ItemsErrored++;
                        context.Logger.LogError($"Saving locale assignment failed; Locale ID: {locale.ID}, Buyer ID: {buyerId}, User Group ID: {userGroupId}\n{ex.Message}\n{ex}");
                    }
                }
            }
        }

    }
}