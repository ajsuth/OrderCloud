// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CatalogExportPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    /// <summary>Defines the catalog export policy.</summary>
    /// <seealso cref="Policy" />
    public class CatalogExportPolicy : Policy
    {
        /// <summary>
        /// Gets or sets the catalog name to export.
        /// </summary>
        /// <value>
        /// The catalog name.
        /// </value>
        public string CatalogName { get; set; }

        /// <summary>
        /// Gets or sets the buyer identifier for catalog assignments.
        /// </summary>
        /// <value>
        /// The buyer identifier.
        /// </value>
        public string DefaultBuyerId { get; set; }

        /// <summary>
        /// Gets or sets the Storefront name from /sitecore/Commerce/Commerce Control Panel/Storefront Settings/Storefronts/&lt;storefront&gt;
        /// </summary>
        /// <value>
        /// The storefront name.
        /// </value>
        public string ShopName { get; set; }

        /// <summary>
        /// Gets or sets the currencies for the catalog. Will create specific buyer user groups for each currency.
        /// </summary>
        /// <value>
        /// The catalog's currencies.
        /// </value>
        public List<string> Currencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the default currency for the catalog. Will determine the default buyer user group buyer users will be assigned to.
        /// </summary>
        /// <value>
        /// The catalog's default currency.
        /// </value>
        public string DefaultCurrency { get; set; }
    }
}
