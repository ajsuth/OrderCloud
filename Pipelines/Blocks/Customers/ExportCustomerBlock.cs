// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCustomerBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OrderCloud.SDK;
using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCustomer pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCustomer)]
    public class ExportCustomerBlock : AsyncPipelineBlock<Customer, Customer, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportCustomerBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportCustomerBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Customer"/>.</returns>
        public override async Task<Customer> RunAsync(Customer customer, CommercePipelineExecutionContext context)
        {
            Condition.Requires(customer).IsNotNull($"{Name}: The customer can not be null");

            var client = context.CommerceContext.GetObject<OrderCloudClient>();
            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            var buyerId = customer.Domain;

            var buyer = await GetOrCreateBuyer(client, buyerId, context, exportResult);
            if (buyer == null)
            {
                return null;
            }

            var securityProfile = await GetOrCreateSecurityProfile(client, buyerId, context, exportResult);
            if (securityProfile != null)
            {
                await GetOrCreateSecurityProfileAssignment(client, buyerId, context, exportResult);
            }

            var user = await UpdateBuyerUser(client, customer, buyerId, context, exportResult);
            if (user == null)
            {
                return null;
            }

            return customer;
        }

        /// <summary>
        /// Gets or creates a buyer.
        /// </summary>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="buyerId">The buyer identifier.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Buyer"/>.</returns>
        protected async Task<Buyer> GetOrCreateBuyer(OrderCloudClient client, string buyerId, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                var buyer = context.CommerceContext.GetObjects<Buyer>().FirstOrDefault(b => b.ID == buyerId);

                if (buyer != null)
                {
                    return buyer;
                }

                exportResult.Buyers.ItemsProcessed++;

                buyer = await client.Buyers.GetAsync(buyerId);
                exportResult.Buyers.ItemsNotChanged++;

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

                        buyer = await client.Buyers.SaveAsync(buyerId, buyer);
                        exportResult.Buyers.ItemsCreated++;

                        return buyer;
                    }
                    catch (Exception e)
                    {
                        exportResult.Buyers.ItemsErrored++;

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
                    exportResult.Buyers.ItemsErrored++;

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
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="SecurityProfile"/>.</returns>
        protected async Task<SecurityProfile> GetOrCreateSecurityProfile(OrderCloudClient client, string profileId, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                var securityProfile = context.CommerceContext.GetObjects<SecurityProfile>().FirstOrDefault(b => b.ID == profileId);

                if (securityProfile != null)
                {
                    return securityProfile;
                }

                exportResult.SecurityProfiles.ItemsProcessed++;

                securityProfile = await client.SecurityProfiles.GetAsync(profileId);
                exportResult.SecurityProfiles.ItemsNotChanged++;

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

                        securityProfile = await client.SecurityProfiles.SaveAsync(profileId, securityProfile);
                        exportResult.SecurityProfiles.ItemsCreated++;

                        return securityProfile;
                    }
                    catch (Exception e)
                    {
                        exportResult.SecurityProfiles.ItemsErrored++;

                        context.Logger.LogError($"{Name}: Create security profile '{profileId}' failed.\n{e.Message}\n{e}");
                    }
                }
                else
                {
                    exportResult.SecurityProfiles.ItemsErrored++;

                    context.Logger.LogError($"{Name}: Get security profile '{profileId}' failed.\n{ex.Message}\n{ex}");
                }
            }

            return null;
        }

        /// <summary>
        /// Gets or creates a security profile assignment.
        /// </summary>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="profileId">The buyer/profile identifier.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="SecurityProfileAssignment"/>.</returns>
        protected async Task GetOrCreateSecurityProfileAssignment(OrderCloudClient client, string profileId, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                var securityProfileAssignment = new SecurityProfileAssignment
                {
                    SecurityProfileID = profileId,
                    BuyerID = profileId
                };

                exportResult.SecurityProfileAssignments.ItemsProcessed++;

                await client.SecurityProfiles.SaveAssignmentAsync(securityProfileAssignment);
                exportResult.SecurityProfileAssignments.ItemsCreated++;
            }
            catch (Exception e)
            {
                exportResult.SecurityProfileAssignments.ItemsErrored++;

                context.Logger.LogError($"{Name}: Create security profile assignment '{profileId}' failed.\n{e.Message}\n{e}");
            }
        }

        protected async Task<User> UpdateBuyerUser(OrderCloudClient client, Customer customer, string buyerId, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                var userPolicy = context.GetPolicy<UserPolicy>();
                var user = new User
                {
                    ID = customer.FriendlyId,
                    Username = customer.LoginName,
                    FirstName = !string.IsNullOrWhiteSpace(customer.FirstName) ? customer.FirstName : userPolicy.DefaultFirstName,
                    LastName = !string.IsNullOrWhiteSpace(customer.LastName) ? customer.LastName : userPolicy.DefaultLastName,
                    Email = customer.Email,
                    Active = customer.AccountStatus == context.GetPolicy<KnownCustomersStatusesPolicy>().ActiveAccount
                };

                user = await client.Users.SaveAsync(buyerId, user.ID, user);
                exportResult.BuyerUsers.ItemsUpdated++;

                return user;
            }
            catch (Exception ex)
            {
                exportResult.BuyerUsers.ItemsErrored++;
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        OrderCloudConstants.Errors.UpdateBuyerUserFailed,
                        new object[]
                        {
                            Name,
                            customer.Id,
                            ex.Message,
                            ex
                        },
                        $"{Name}: Ok| Exporting customer '{customer.Id}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                    context);

                return null;
            }
        }
    }
}