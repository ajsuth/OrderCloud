// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariationSummary.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the VariationSummary model.</summary>
    /// <seealso cref="Model" />
    public class VariationSummary : Model
    {
        /// <summary>
        /// Gets or sets the variation property value.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the variation property value.
        /// </summary>
        public List<VariationProperty> VariationProperties { get; set; } = new List<VariationProperty>();
    }
}