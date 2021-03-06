using EdjCase.ICP.Agent.Identity;
using EdjCase.ICP.Candid.Models;
using EdjCase.ICP.Candid.Utilities;
using Snapshooter.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ICP.Candid.Tests
{
	public class IdentityTests
	{
		[Theory]
		[InlineData("303c300c060a2b0601040183b8430102032c000a00000000000000070101a451d1829b843e2aabdd49ea590668978a73612067bdde0b8502f844452a7558")]
		public void ED25519PublicKey_GetDerEncodedBytes(string publicKeyHex)
		{
			byte[] publicKeyBytes = ByteUtil.FromHexString(publicKeyHex);
			var key = new ED25519PublicKey(publicKeyBytes);
			byte[] derEncoded = key.GetDerEncodedBytes();
			Snapshot.Match(derEncoded);
		}
		[Theory]
		[InlineData("302a300506032b65700321005987fc68902ecf4644fe6c7d82b9f0e957817a8f9f8c58da5d2c9d3d19915229", "d8c5d3d2bcb82f16b399d40ed6ff1801bc5eb31fc301dc1ad517e2ed8c3f268e5987fc68902ecf4644fe6c7d82b9f0e957817a8f9f8c58da5d2c9d3d19915229")]
		public void ED25519Identity_GetDerEncodedBytes(string publicKeyHex, string privateKeyHex)
		{
			byte[] publicKeyBytes = ByteUtil.FromHexString(publicKeyHex);
			var publicKey = new ED25519PublicKey(publicKeyBytes);
			byte[] privateKeyBytes = ByteUtil.FromHexString(privateKeyHex);
			var identity = new ED25519Identity(publicKey, privateKeyBytes);
			var edPublicKey = Assert.IsType<ED25519PublicKey>(publicKey);
			Snapshot.Match(edPublicKey.Value);
		}
	}
}
