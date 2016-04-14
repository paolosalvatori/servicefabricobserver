// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Filters
{
    public class ExpressionFilter
    {
        #region Public Properties

        public string Property { get; set; }

        public Operator Operator { get; set; }

        public object Value { get; set; }

        public LogicalOperator LogicalOperator { get; set; }

        #endregion
    }
}