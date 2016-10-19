#region Copyright

// //=======================================================================================
// // Microsoft Azure Customer Advisory Team  
// //
// // This sample is supplemental to the technical guidance published on the community
// // blog at http://blogs.msdn.com/b/paolos/. 
// // 
// // Author: Paolo Salvatori
// //=======================================================================================
// // Copyright © 2016 Microsoft Corporation. All rights reserved.
// // 
// // THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// // EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// // MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// //=======================================================================================

#endregion

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Filters;
    using Newtonsoft.Json.Linq;

    #endregion

    public class ObserverInfo
    {
        #region Public Constructors

        /// <summary>
        ///     Initializes a new instance of the ObserverInfo class.
        /// </summary>
        public ObserverInfo()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the ObserverInfo class.
        /// </summary>
        /// <param name="filterExpressions">The filter expressions.</param>
        /// <param name="entityId">The entity id.</param>
        public ObserverInfo(IEnumerable<string> filterExpressions, EntityId entityId)
        {
            IList<string> expressions = filterExpressions as IList<string> ?? filterExpressions.ToList();
            this.FilterExpressions = expressions;
            this.EntityId = entityId;
            if (!expressions.Any())
                return;
            ExpressionBuilder<JObject> expressionBuilder = new ExpressionBuilder<JObject>();
            this.Predicates = from expression in expressions
                where !string.IsNullOrWhiteSpace(expression)
                select expressionBuilder.GetExpression(expression).Compile();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the filter expressions.
        /// </summary>
        public IEnumerable<string> FilterExpressions { get; set; }

        /// <summary>
        ///     Gets or sets the entity id.
        /// </summary>
        public EntityId EntityId { get; set; }

        /// <summary>
        ///     Gets the predicates used to evaluate the filter expressions.
        /// </summary>
        public IEnumerable<Func<JObject, bool>> Predicates { get; private set; }

        #endregion
    }
}