// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomerExportPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    public class CustomerExportPolicy : Policy
    {
        /// <summary>
        /// Gets or sets the buyer id.
        /// </summary>
        /// <value>
        /// The buyer id.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the currencies for the buyer.
        /// </summary>
        /// <value>
        /// The buyer's currencies.
        /// </value>
        public List<string> Currencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the default currency for the buyer.
        /// </summary>
        /// <value>
        /// The buyer's default currency.
        /// </value>
        public string DefaultCurrency { get; set; }
    }
}
