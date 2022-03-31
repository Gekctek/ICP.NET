﻿using ICP.Common.Crypto;
using ICP.Common.Models;
using System;
using System.Collections.Generic;

namespace ICP.Agent.Requests
{
    public class QueryRequest : IRepresentationIndependentHashItem
    {
        public string RequestType { get; } = "query";

        /// <summary>
        /// The principal of the canister to call.
        /// </summary>
        public PrincipalId CanisterId { get; }

        /// <summary>
        /// Name of the canister method to call
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Argument to pass to the canister method
        /// </summary>
        public EncodedArgument EncodedArgument { get; }

        /// <summary>
        /// Required. The user who issued the request.
        /// </summary>
        public PrincipalId Sender { get; }

        /// <summary>
        /// Required.  An upper limit on the validity of the request, expressed in nanoseconds since 
        /// 1970-01-01 (like ic0.time()). This avoids replay attacks: The IC will not accept requests, 
        /// or transition requests from status received to status processing, if their expiry date is in 
        /// the past. The IC may refuse to accept requests with an ingress expiry date too far in the future. 
        /// This applies to synchronous and asynchronous requests alike (and could have been called request_expiry).
        /// </summary>
        public ICTimestamp IngressExpiry { get; }

        /// <summary>
        /// Optional. Arbitrary user-provided data, typically randomly generated. 
        /// This can be used to create distinct requests with otherwise identical fields.
        /// </summary>
        public byte[]? Nonce { get; }

        public QueryRequest(PrincipalId canisterId, string method, EncodedArgument encodedArgument, PrincipalId sender, ICTimestamp ingressExpiry)
        {
            this.CanisterId = canisterId ?? throw new ArgumentNullException(nameof(canisterId));
            this.Method = method ?? throw new ArgumentNullException(nameof(method));
            this.EncodedArgument = encodedArgument ?? throw new ArgumentNullException(nameof(encodedArgument));
            this.Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            this.IngressExpiry = ingressExpiry ?? throw new ArgumentNullException(nameof(ingressExpiry));
        }

        public Dictionary<string, IHashable> BuildHashableItem()
        {
            return new Dictionary<string, IHashable>
            {
                {Properties.REQUEST_TYPE, this.RequestType.ToHashable()},
                {Properties.CANNISTER_ID, this.CanisterId},
                {Properties.METHOD_NAME, this.Method.ToHashable()},
                {Properties.ARG, this.EncodedArgument},
                {Properties.SENDER, this.Sender},
                {Properties.INGRESS_EXPIRY, this.IngressExpiry}
            };
        }


        private static class Properties
        {
            public const string REQUEST_TYPE = "request_type";
            public const string CANNISTER_ID = "cannister_id";
            public const string METHOD_NAME = "method_name";
            public const string ARG = "arg";
            public const string SENDER = "sender";
            public const string INGRESS_EXPIRY = "ingress_expiry";
            public const string NONCE = "nonce";
        }
    }
}