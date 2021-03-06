using EdjCase.ICP.Candid;
using EdjCase.ICP.Candid.Models;
using EdjCase.ICP.Candid.Models.Types;
using EdjCase.ICP.Candid.Models.Values;
using EdjCase.ICP.ClientGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICP.ClientGenerator
{
	internal static class TypeSourceGenerator
	{
		private static Dictionary<Type, string> systemTypeShorthands = new Dictionary<Type, string>
		{
			{ typeof(string), "string" },
			{ typeof(byte), "byte" },
			{ typeof(ushort), "ushort" },
			{ typeof(uint), "uint" },
			{ typeof(ulong), "ulong" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(short), "short" },
			{ typeof(int), "int" },
			{ typeof(long), "long" },
			{ typeof(float), "float" },
			{ typeof(double), "double" },
			{ typeof(bool), "bool" }
		};

		public static string GenerateClientSourceCode(string baseNamespace, ServiceSourceDescriptor desc)
		{
			IndentedStringBuilder builder = new();
			builder.AppendLine("using EdjCase.ICP.Agent.Agents;");
			builder.AppendLine("using EdjCase.ICP.Agent.Responses;");
			builder.AppendLine("using EdjCase.ICP.Agent.Auth;");
			builder.AppendLine("using EdjCase.ICP.Candid.Models;");
			builder.AppendLine($"using {baseNamespace}.Models;");
			builder.AppendLine("");

			WriteNamespace(builder, baseNamespace, () =>
			{
				WriteService(builder, desc);
			});
			return BuildSourceWithShorthands(builder);
		}
		public static (string FileName, string SourceCode) GenerateTypeSourceCode(string baseNamespace, TypeSourceDescriptor type)
		{
			IndentedStringBuilder builder = new();


			WriteNamespace(builder, baseNamespace + ".Models", () =>
			{
				WriteType(builder, type);
			});
			string source = BuildSourceWithShorthands(builder);
			return (type.Name, source);
		}

		public static string GenerateAliasSourceCode(Dictionary<string, string> aliases)
		{
			IndentedStringBuilder builder = new();
			foreach ((string id, string aliasedType) in aliases)
			{
				builder.AppendLine($"global using {id} = {aliasedType};");
			}
			return BuildSourceWithShorthands(builder);
		}

		private static void WriteType(IndentedStringBuilder builder, TypeSourceDescriptor type)
		{
			switch (type)
			{
				case VariantSourceDescriptor v:
					WriteVariant(builder, v);
					break;
				case RecordSourceDescriptor r:
					WriteRecord(builder, r);
					break;
				case ServiceSourceDescriptor s:
					WriteService(builder, s);
					break;
				default:
					throw new NotImplementedException();
			};
		}

		private static string BuildSourceWithShorthands(IndentedStringBuilder builder)
		{
			string source = builder.ToString();
			foreach ((Type systemType, string shortHand) in systemTypeShorthands)
			{
				// Convert system types to shorten versions
				string fullTypeName = systemType.FullName ?? throw new Exception($"Type '{systemType}' has no full name");
				source = source.Replace(fullTypeName, shortHand);
			}
			return source;
		}

		private static void WriteService(IndentedStringBuilder builder, ServiceSourceDescriptor service)
		{
			string className = $"{service.Name}ApiClient";
			WriteClass(builder, className, () =>
			{
				builder.AppendLine("public IAgent Agent { get; }");
				builder.AppendLine("public Principal CanisterId { get; }");
				// Constrcutor
				WriteMethod(
					builder,
					inner: () =>
					{
						builder.AppendLine("this.Agent = agent ?? throw new ArgumentNullException(nameof(agent));");
						builder.AppendLine("this.CanisterId = canisterId ?? throw new ArgumentNullException(nameof(canisterId));");
					},
					access: "public",
					isStatic: false,
					isAsync: false,
					isConstructor: true,
					returnType: null,
					name: className,
					baseConstructorParams: null,
					("IAgent", "agent"),
					("Principal", "canisterId")
				);
				foreach (ServiceSourceDescriptor.Method func in service.Methods)
				{
					List<(string TypeName, string VariableName)> args = func.Parameters
						.Where(p => p.TypeName != null) // exclude null/empty/reserved
						.Select((a, i) => (a.TypeName!, a.VariableName))
						.ToList();
					args.Add(("IIdentity?", "identityOverride = null"));
					List<(string Type, string Param)> returnTypes;
					if (func.IsFireAndForget)
					{
						// TODO confirm no return types, or even not async?
						returnTypes = new List<(string Type, string Param)>();
					}
					else
					{
						returnTypes = func.ReturnParameters
							.Where(p => p.TypeName != null) // exclude null/empty/reserved
							.Select(p => (p.TypeName!, p.VariableName))
							.ToList();
					}
					WriteMethod(
						builder,
						() =>
						{
							builder.AppendLine($"string method = \"{func.UnmodifiedName}\";");

							var parameterVariables = new List<string>();
							foreach (ServiceSourceDescriptor.Method.ParameterInfo parameter in func.Parameters)
							{
								int index = parameterVariables.Count;
								string variableName = "p" + index;
								string valueWithType;
								if (parameter.TypeName != null)
								{
									valueWithType = $"CandidValueWithType.FromObject<{parameter.TypeName}>({parameter.VariableName})";
								}
								else
								{
									valueWithType = "CandidValueWithType.Null()";
								}
								builder.AppendLine($"CandidValueWithType {variableName} = {valueWithType};");
								parameterVariables.Add(variableName);
							}

							builder.AppendLine("var candidArgs = new List<CandidValueWithType>");
							builder.AppendLine("{");
							using (builder.Indent())
							{
								foreach (string v in parameterVariables)
								{
									builder.AppendLine(v + ",");
								}
							}
							builder.AppendLine("};");
							builder.AppendLine("CandidArg arg = CandidArg.FromCandid(candidArgs);");
							builder.AppendLine("QueryResponse response = await this.Agent.QueryAsync(this.CanisterId, method, arg, identityOverride);");
							builder.AppendLine("QueryReply reply = response.ThrowOrGetReply();");

							if (returnTypes.Any())
							{
								var returnParamVariables = new List<string>();
								int i = 0;
								foreach (ServiceSourceDescriptor.Method.ParameterInfo parameter in func.ReturnParameters)
								{
									// Only include non null/empty/reserved params
									if (parameter.TypeName != null)
									{
										string variableName = "r" + i;
										string? orDefault = parameter.TypeName.EndsWith("?") ? "OrDefault" : null; // TODO better detection of optional
										builder.AppendLine($"{parameter.TypeName} {variableName} = reply.Arg.Values[{i}].ToObject{orDefault}<{parameter.TypeName}>();");
										returnParamVariables.Add(variableName);
									}
									i++;
								}
								string returnString = string.Join(", ", returnParamVariables);
								builder.AppendLine($"return ({returnString});");
							}

						},
						access: "public",
						isStatic: false,
						isAsync: true,
						isConstructor: false,
						returnTypes: returnTypes,
						name: func.Name + "Async",
						baseConstructorParams: null,
						args.ToArray()
					);

				}
			});
		}


		private static string BuildCandidId(CandidId? id)
		{
			if (id == null)
			{
				return "null";
			}
			return $"CandidId.Parse(\"{id}\")";
		}

		private static string BuildCandidTag(CandidTag tag)
		{
			return $"new CandidTag(\"{tag.Id}\", {$"\"tag.Name\"" ?? "null"})";
		}

		private static string BuildDictionaryString(string genericType1, string genericType2, IEnumerable<(string, string)> values)
		{
			string valuesString = string.Join(", ", values.Select(v => $"{{ {v.Item1}, {v.Item2} }}"));
			return $"new Dictionary<CandidTag, CandidType > {{ {valuesString} }}";
		}

		private static string BuildListString(string genericType, IEnumerable<string> values)
		{
			string valuesString = string.Join(", ", values);
			return $"new List<{genericType}> {{ {valuesString} }}";
		}

		private static void WriteRecord(IndentedStringBuilder builder, RecordSourceDescriptor record)
		{
			string className = record.Name;
			WriteClass(builder, className, () =>
			{
				foreach ((string fieldName, string fieldFullTypeName) in record.Fields)
				{
					builder.AppendLine($"public {fieldFullTypeName} {fieldName} {{ get; set; }}");
					builder.AppendLine("");
				}

				foreach (TypeSourceDescriptor paramType in record.SubTypesToCreate)
				{
					WriteType(builder, paramType);
				}

			});

		}

		private static void WriteVariant(IndentedStringBuilder builder, VariantSourceDescriptor variant)
		{
			string enumName = $"{variant.Name}Type";
			string className = variant.Name;
			List<string> enumValues = variant.Options
				.Select(o => o.Name)
				.ToList();
			WriteEnum(builder, enumName, enumValues);
			var implementationTypes = new List<string>
			{
				$"EdjCase.ICP.Candid.CandidVariantValueBase<{enumName}>"
			};
			WriteClass(builder, className, () =>
			{
				// Constrcutor
				WriteMethod(
					builder,
					inner: () =>
					{
					},
					access: "public",
					isStatic: false,
					isAsync: false,
					isConstructor: true,
					returnType: null,
					name: className,
					baseConstructorParams: new List<string> { "type", "value" },
					(enumName, "type"),
					("object?", "value")
				);
				builder.AppendLine("");

				// Empty Constrcutor for reflection
				WriteMethod(
					builder,
					inner: () =>
					{
					},
					access: "protected",
					isStatic: false,
					isAsync: false,
					isConstructor: true,
					returnType: null,
					name: className,
					baseConstructorParams: null
				);
				builder.AppendLine("");



				foreach ((string optionName, string? infoFullTypeName) in variant.Options)
				{
					if (infoFullTypeName == null)
					{
						WriteMethod(
							builder,
							inner: () =>
							{
								builder.AppendLine($"return new {className}({enumName}.{optionName}, null);");
							},
							access: "public",
							isStatic: true,
							isAsync: false,
							isConstructor: false,
							returnType: className,
							name: optionName
						);
					}
					else
					{
						WriteMethod(
							builder,
							inner: () =>
							{
								builder.AppendLine($"return new {className}({enumName}.{optionName}, info);");
							},
							access: "public",
							isStatic: true,
							isAsync: false,
							isConstructor: false,
							returnType: className,
							name: optionName,
					baseConstructorParams: null,
							(infoFullTypeName, "info")
						);
						builder.AppendLine("");

						WriteMethod(
							builder,
							inner: () =>
							{
								builder.AppendLine($"this.ValidateType({enumName}.{optionName});");
								builder.AppendLine($"return ({infoFullTypeName})this.value!;");
							},
							access: "public",
							isStatic: false,
							isAsync: false,
							isConstructor: false,
							returnType: infoFullTypeName,
							name: "As" + optionName
						);
					}
					builder.AppendLine("");

				}




				foreach (TypeSourceDescriptor paramType in variant.SubTypesToCreate)
				{
					WriteType(builder, paramType);
				}

			}, implementationTypes);
		}



		private static void WriteNamespace(IndentedStringBuilder builder, string name, Action inner)
		{
			builder.AppendLine($"namespace {name}");
			builder.AppendLine("{");
			using (builder.Indent())
			{
				inner();
			}
			builder.AppendLine("}");
		}

		private static void WriteMethod(
			IndentedStringBuilder builder,
			Action inner,
			string? access,
			bool isStatic,
			bool isAsync,
			bool isConstructor,
			string? returnType,
			string name,
			List<string>? baseConstructorParams = null,
			params (string Type, string Param)[] parameters)
		{
			List<(string, string)> returnTypes = new();
			if (returnType != null)
			{
				returnTypes.Add((returnType, returnType));
			}
			WriteMethod(builder, inner, access, isStatic, isAsync, isConstructor, returnTypes, name, baseConstructorParams, parameters);
		}

		private static void WriteMethod(
			IndentedStringBuilder builder,
			Action inner,
			string? access,
			bool isStatic,
			bool isAsync,
			bool isConstructor,
			List<(string Type, string Param)> returnTypes,
			string name,
			List<string>? baseConstructorParams = null,
			params (string Type, string Param)[] parameters)
		{
			var methodItems = new List<string>();
			if (access != null)
			{
				methodItems.Add(access);
			}
			if (!isConstructor)
			{
				if (isStatic)
				{
					methodItems.Add("static");
				}

				string returnValue;
				if (!returnTypes.Any())
				{
					returnValue = "void";
				}
				else if (returnTypes.Count == 1)
				{
					returnValue = returnTypes
						.Select(r => r.Type)
						.Single();
				}
				else
				{
					returnValue = $"({string.Join(", ", returnTypes.Select(r => $"{r.Type} {r.Param}"))})";
				}
				if (isAsync)
				{
					if (!returnTypes.Any())
					{
						returnValue = "async Task";
					}
					else
					{
						returnValue = $"async Task<{returnValue}>";
					}
				}

				methodItems.Add(returnValue);
			}
			string parametersString = string.Join(", ", parameters.Select(p => $"{p.Type} {p.Param}"));


			methodItems.Add($"{name}({parametersString})");

			if (isConstructor)
			{
				if (baseConstructorParams != null)
				{
					methodItems.Add($" : base({string.Join(", ", baseConstructorParams)})");
				}
			}
			builder.AppendLine($"{string.Join(" ", methodItems)}");
			builder.AppendLine("{");
			using (builder.Indent())
			{
				inner();
			}
			builder.AppendLine("}");
		}


		private static void WriteEnum(IndentedStringBuilder builder, string name, List<string> values)
		{
			builder.AppendLine($"public enum {name}");
			builder.AppendLine("{");
			using (builder.Indent())
			{
				foreach (string v in values)
				{
					builder.AppendLine(v + ",");
				}
			}
			builder.AppendLine("}");
		}

		private static void WriteClass(IndentedStringBuilder builder, string name, Action inner, List<string>? implementTypes = null)
		{
			string? implementations = null;
			if (implementTypes?.Any() == true)
			{
				implementations = " : " + string.Join(", ", implementTypes);
			}
			builder.AppendLine($"public class {name}{implementations}");
			builder.AppendLine("{");
			using (builder.Indent())
			{
				inner();
			}
			builder.AppendLine("}");
		}
	}
}
