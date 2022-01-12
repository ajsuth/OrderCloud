// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariationProperty.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the VariationProperty model.</summary>
    /// <seealso cref="Model" />
    public class VariationProperty : Model
    {
        /// <summary>
        /// Gets or sets the variation property value.
        /// </summary>
        public string Value { get; set; }
    }
}