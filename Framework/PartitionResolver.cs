// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    using System;
    using System.Data.HashFunction;

    public static class PartitionResolver
    {
        #region Private Constants

        private const uint DefaultSeed = 54325U;
        private const int DefaultPartitionCount = 3;

        #endregion

        #region Public Static Methods

        public static long Resolve(string input)
        {
            return Resolve(input, DefaultPartitionCount);
        }

        public static long Resolve(string input, int partitions)
        {
            MurmurHash3 hasher = new MurmurHash3(32, DefaultSeed);
            byte[] hashRaw = hasher.ComputeHash(input);
            uint fullHash = BitConverter.ToUInt32(hashRaw, 0);
            long hash = fullHash%partitions;
            return hash + 1;
        }

        #endregion
    }
}