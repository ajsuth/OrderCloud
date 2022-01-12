// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CatalogExportPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

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
        /// Gets or sets if multi-currency is configured for the catalog. Will create specific buyer user groups for each currency.
        /// </summary>
        /// <value>
        /// The multi-currency.
        /// </value>
        public bool MultiCurrency { get; set; }
    }
}
