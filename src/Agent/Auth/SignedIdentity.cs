using EdjCase.ICP.Agent.Identity;
using EdjCase.ICP.Candid.Crypto;
using EdjCase.ICP.Candid.Models;
using EdjCase.ICP.Candid.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.ICP.Agent.Auth
{
	public abstract class SigningIdentityBase : IIdentity
	{
		public abstract IPublicKey GetPublicKey();
		public virtual List<SignedDelegation>? GetSenderDelegations()
		{
			return null;
		}

		public abstract Signature Sign(byte[] sign);

		public Principal GetPrincipal()
		{
			IPublicKey publicKey = this.GetPublicKey();
			return Principal.SelfAuthenticating(publicKey);
		}

		public SignedContent CreateSignedContent(Dictionary<string, IHashable> content)
		{
			IPublicKey senderPublicKey = this.GetPublicKey();
			byte[] domainSeparator = Encoding.UTF8.GetBytes("\x0Aic-request");
			var sha256 = SHA256HashFunction.Create();
			byte[] contentHash = content.ToHashable().ComputeHash(sha256);
			Signature senderSignature = this.Sign(domainSeparator.Concat(contentHash).ToArray());
			List<SignedDelegation>? senderDelegations = this.GetSenderDelegations();
			return new SignedContent(content, senderPublicKey.GetRawBytes(), senderDelegations, senderSignature);
		}
	}
}
