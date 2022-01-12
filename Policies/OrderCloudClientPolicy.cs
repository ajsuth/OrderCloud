// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrderCloudClientPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    /// <summary>Defines the OrderCloudClient policy.</summary>
    /// <seealso cref="Policy" />
    public class OrderCloudClientPolicy : Policy
    {
        /// <summary>
        /// Gets or sets the API URL.
        /// </summary>
        /// <value>
        /// The API URL.
        /// </value>
        public string ApiUrl { get; set; } = "https://sandboxapi.ordercloud.io";

        /// <summary>
        /// Gets or sets the authorization URL.
        /// </summary>
        /// <value>
        /// The authorization URL.
        /// </value>
        public string AuthUrl { get; set; } = "https://sandboxapi.ordercloud.io";

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        /// <value>
        /// The client secret.
        /// </value>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Returns true if all properties have values.
        /// </summary>
        /// <returns>Returns true if all properties have values.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ApiUrl)
                && !string.IsNullOrEmpty(AuthUrl)
                && !string.IsNullOrEmpty(ClientId)
                && !string.IsNullOrEmpty(ClientSecret);
        }
    }
}
