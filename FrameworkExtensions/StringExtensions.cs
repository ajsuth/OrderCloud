// <copyright file="StringExtensions.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions
{
    /// <summary>
    /// Defines extensions for <see cref="string"/>
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex NonAlphaNumericRegex = new Regex("[^\\w]", RegexOptions.Compiled);

        /// <summary>
        /// Sanitizes the identifier input to a valid OrderCloud identifier.
        /// </summary>
        /// <param name="identifier">The identifier to be sanitized.</param>
        /// <returns>A valid OrderCloud identifier</returns>
        public static string ToValidOrderCloudId(this string identifier)
        {
            return NonAlphaNumericRegex.Replace(identifier, "_");
        }
    }
}
