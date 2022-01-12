// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportObject.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the ExportObject model.</summary>
    public class VariantXp
    {
        public IList<Tag> Tags { get; set; } = new List<Tag>();
        public List<string> PriceSchedules { get; set; } = new List<string>();
    }
}