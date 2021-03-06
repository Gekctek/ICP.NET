using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using EdjCase.ICP.Agent;
using EdjCase.ICP.Candid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Cbor
{
	internal class SignatureCborConverter : CborConverterBase<Signature?>
	{
		public override Signature? Read(ref CborReader reader)
		{
			if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
			{
				return null;
			}
			if (!reader.TryReadSemanticTag(out ulong tag) && tag != 55799)
			{
				throw new CborException($"Expected semantic tag '{55799}' for signature, got '{tag}'");
			}
			ReadOnlySpan<byte> bytes = reader.ReadByteString();

			return new Signature(bytes.ToArray());
		}

		public override void Write(ref CborWriter writer, Signature? value)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteByteString(value.Value);
		}
	}
}
