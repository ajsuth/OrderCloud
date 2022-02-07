// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportBuyerBlock.cs" company="Sitecore Corporation">
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
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.Shops;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportBuyer pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportBuyer)]
    public class ExportBuyerBlock : AsyncPipelineBlock<Shop, Shop, CommercePipelineExecutionContext>
    {
        /// <summary>The commerce commander.</summary>
        protected CommerceCommander Commander { get; set; }

        /// <summary>The OrderCloud client.</summary>
        protected OrderCloudClient Client { get; set; }

        /// <summary>The export result model.</summary>
        protected ExportResult Result { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportBuyerBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportBuyerBlock(CommerceCommander commander)
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

            var buyerId = shop.Id.ToValidOrderCloudId();

            var buyer = await GetOrCreateBuyer(context, buyerId);
            if (buyer == null)
            {
                return null;
            }

            var securityProfile = await GetOrCreateSecurityProfile(context, buyerId);
            if (securityProfile != null)
            {
                await CreateOrUpdateSecurityProfileAssignment(context, buyerId);
            }

            await CreateOrUpdateBuyerUserGroups(context, buyerId, shop);

            var locales = await CreateOrUpdateLocales(context, shop);
            if (locales != null)
            {
                await CreateOrUpdateLocaleAssignments(context, buyerId, shop, locales);
            }

            return shop;
        }

        /// <summary>
        /// Gets or creates a buyer.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buyerId">The buyer identifier.</param>
        /// <returns>The <see cref="Buyer"/>.</returns>
        protected async Task<Buyer> GetOrCreateBuyer(CommercePipelineExecutionContext context, string buyerId)
        {
            try
            {
                var buyer = context.CommerceContext.GetObjects<Buyer>().FirstOrDefault(b => b.ID == buyerId);

                if (buyer != null)
                {
                    return buyer;
                }

                Result.Buyers.ItemsProcessed++;

                buyer = await Client.Buyers.GetAsync(buyerId);
                Result.Buyers.ItemsNotChanged++;

                context.CommerceContext.AddObject(buyer);
                
                return buyer;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    try
                    {
                        var buyer = new Buyer
                        {
                            ID = buyerId,
                            Active = true,
                            Name = buyerId
                        };

                        context.Logger.LogInformation($"Saving buyer; Buyer ID: {buyerId}");
                        buyer = await Client.Buyers.SaveAsync(buyerId, buyer);
                        Result.Buyers.ItemsCreated++;

                        return buyer;
                    }
                    catch (Exception e)
                    {
                        Result.Buyers.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                OrderCloudConstants.Errors.CreateBuyerFailed,
                                new object[]
                                {
                                    Name,
                                    buyerId,
                                    e.Message,
                                    e
                                },
                                $"{Name}: Ok| Create buyer '{buyerId}' failed.\n{e.Message}\n{e}").ConfigureAwait(false),
                            context);
                    }
                }
                else
                {
                    Result.Buyers.ItemsErrored++;

                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.GetBuyerFailed,
                            new object[]
                            {
                                Name,
                                buyerId,
                                ex.Message,
                                ex
                            },
                            $"{Name}: Ok| Get buyer '{buyerId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                        context);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets or creates a security profile.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="profileId">The profile identifier.</param>
        /// <returns>The <see cref="SecurityProfile"/>.</returns>
        protected async Task<SecurityProfile> GetOrCreateSecurityProfile(CommercePipelineExecutionContext context, string profileId)
        {
            try
            {
                var securityProfile = context.CommerceContext.GetObjects<SecurityProfile>().FirstOrDefault(b => b.ID == profileId);

                if (securityProfile != null)
                {
                    return securityProfile;
                }

                Result.SecurityProfiles.ItemsProcessed++;

                securityProfile = await Client.SecurityProfiles.GetAsync(profileId);
                Result.SecurityProfiles.ItemsNotChanged++;

                context.CommerceContext.AddObject(securityProfile);

                return securityProfile;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    try
                    {
                        var securityProfile = new SecurityProfile
                        {
                            ID = profileId,
                            Name = profileId,
                            Roles = new List<ApiRole>() { ApiRole.MeAddressAdmin, ApiRole.MeAdmin, ApiRole.MeCreditCardAdmin, ApiRole.MeXpAdmin, ApiRole.PasswordReset, ApiRole.Shopper }
                        };

                        context.Logger.LogInformation($"Saving security profile; Security Profile ID: {profileId}");
                        securityProfile = await Client.SecurityProfiles.SaveAsync(profileId, securityProfile);
                        Result.SecurityProfiles.ItemsCreated++;

                        return securityProfile;
                    }
                    catch (Exception e)
                    {
                        Result.SecurityProfiles.ItemsErrored++;

                        context.Logger.LogError($"{Name}: Saving security profile '{profileId}' failed.\n{e.Message}\n{e}");
                    }
                }
                else
                {
                    Result.SecurityProfiles.ItemsErrored++;

                    context.Logger.LogError($"{Name}: Get security profile '{profileId}' failed.\n{ex.Message}\n{ex}");
                }
            }

            return null;
        }

        /// <summary>
        /// Creates or updates a security profile assignment.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="profileId">The buyer/profile identifier.</param>
        /// <returns>The <see cref="SecurityProfileAssignment"/>.</returns>
        protected async Task CreateOrUpdateSecurityProfileAssignment(CommercePipelineExecutionContext context, string profileId)
        {
            try
            {
                var securityProfileAssignment = new SecurityProfileAssignment
                {
                    SecurityProfileID = profileId,
                    BuyerID = profileId
                };

                Result.SecurityProfileAssignments.ItemsProcessed++;

                context.Logger.LogInformation($"Saving security profile assignment; Security Profile ID: {profileId}, Buyer ID: {profileId}");
                await Client.SecurityProfiles.SaveAssignmentAsync(securityProfileAssignment);
                Result.SecurityProfileAssignments.ItemsCreated++;
            }
            catch (Exception e)
            {
                Result.SecurityProfileAssignments.ItemsErrored++;
                context.Logger.LogError($"{Name}: Saving security profile assignment '{profileId}' failed.\n{e.Message}\n{e}");
            }
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