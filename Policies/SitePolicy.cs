// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SitePolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    public class SitePolicy : Policy
    {
        /// <summary>
        /// Gets or sets the site name.
        /// </summary>
        /// <value>
        /// The site name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the site's catalog.
        /// </summary>
        /// <value>
        /// The site's catalog.
        /// </value>
        public string Catalog { get; set; }

        /// <summary>
        /// Gets or sets the site's domain.
        /// </summary>
        /// <value>
        /// The site's domain.
        /// </value>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the site's configured storefront name.
        /// </summary>
        /// <value>
        /// The site's configured storefront name.
        /// </value>
        public string Storefront { get; set; }

        /// <summary>
        /// Gets or sets the default user group to assign to buyer users.
        /// </summary>
        /// <value>
        /// The default user group to assign to buyer users.
        /// </value>
        public string DefaultUserGroup { get; set; }
    }
}
