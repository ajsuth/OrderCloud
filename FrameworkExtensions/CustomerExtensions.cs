// <copyright file="CustomerExtensions.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Customers;
using System;
using System.Linq;

namespace Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions
{
    /// <summary>
    /// Defines extensions for <see cref="Customer"/>
    /// </summary>
    public static class CustomerExtensions
    {
        /// <summary>
        /// Determines if Variants will need to be created for the OrderCloud product. Sellable items with a single variation may be folded into a standalone product
        /// if the variation does not have any variation properties configured.
        /// </summary>
        /// <param name="Customer">The <see cref="Customer"/>.</param>
        /// <returns>True if there is more than 1 variant or if the sole variant does not have any variation properties configured.</returns>
        public static EntityView GetCustomerDetailsEntityView(this Customer customer)
        {
            if (customer == null || !customer.HasComponent<CustomerDetailsComponent>())
            {
                return null;
            }

            return customer.GetComponent<CustomerDetailsComponent>().View?.ChildViews?.FirstOrDefault(v => v.Name.Equals("Details", StringComparison.OrdinalIgnoreCase)) as EntityView;;
        }

    }
}
