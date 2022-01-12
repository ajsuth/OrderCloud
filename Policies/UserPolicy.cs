// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    /// <summary>Defines the user policy.</summary>
    /// <seealso cref="Policy" />
    public class UserPolicy : Policy
    {
        /// <summary>
        /// Gets or sets the default first name.
        /// </summary>
        /// <value>
        /// The default first name.
        /// </value>
        public string DefaultFirstName { get; set; } = "FirstName";

        /// <summary>
        /// Gets or sets the default last name.
        /// </summary>
        /// <value>
        /// The default last name.
        /// </value>
        public string DefaultLastName { get; set; } = "LastName";
    }
}
