using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.CodeDom;
using System.CodeDom.Compiler;
using CD;


namespace CodeDomDemo
{
	partial class Program
	{
		static void Demo6()
		{
			// load the xsd
			XmlSchema xsd=null;
			using (FileStream stream = new FileStream("..\\..\\Test.xsd", FileMode.Open, FileAccess.Read))
			{
				xsd = XmlSchema.Read(stream, null);
			}
			
			XmlSchemas xsds = new XmlSchemas();
			xsds.Add(xsd);
			xsds.Compile(null, true);
			XmlSchemaImporter xsdImporter = new XmlSchemaImporter(xsds);

			// create the codedom
			CodeNamespace ns = new CodeNamespace();
			CodeCompileUnit ccu = new CodeCompileUnit();
			ccu.ReferencedAssemblies.Add(typeof(XmlTypeAttribute).Assembly.GetName().ToString());
			ccu.Namespaces.Add(ns);
			XmlCodeExporter codeExporter = new XmlCodeExporter( ns,ccu);

			var maps = new List<XmlTypeMapping>();
			foreach (XmlSchemaType schemaType in xsd.SchemaTypes.Values)
			{
				maps.Add(xsdImporter.ImportSchemaType(schemaType.QualifiedName));
			}
			foreach (XmlSchemaElement schemaElement in xsd.Elements.Values)
			{
				maps.Add(xsdImporter.ImportTypeMapping(schemaElement.QualifiedName));
			}
			foreach (XmlTypeMapping map in maps)
			{
				codeExporter.ExportTypeMapping(map);
			}
			var typeMap = new Dictionary<string, CodeTypeDeclaration>(StringComparer.InvariantCulture);
			_FillTypeMap(ns, typeMap);
			foreach (var kvp in typeMap)
			{
				var td = kvp.Value;
				var propElem = new HashSet<string>(StringComparer.InvariantCulture);
				var propAttr = new HashSet<string>(StringComparer.InvariantCulture);
				var fldMap = new Dictionary<string, string>();
				_FillPropFldMaps(td, propElem, propAttr, fldMap);
				// fix up our xml type attribute
				foreach (CodeAttributeDeclaration d in td.CustomAttributes)
				{
					if(0==string.Compare(d.AttributeType.BaseType, "System.Xml.Serialization.XmlAttributeAttribute", StringComparison.InvariantCulture)) {
						d.Arguments.Insert(0, new CodeAttributeArgument("TypeName", new CodePrimitiveExpression(td.Name)));
						break;
					}
				}
				// correct the type name
				td.Name=_ToNetCase(td.Name);
				CodeDomVisitor.Visit(td, (ctx) =>
				{
					var fldRef = ctx.Target as CodeFieldReferenceExpression;
					if (null != fldRef && fldMap.ContainsKey(fldRef.FieldName))
					{
						fldRef.FieldName = _ToPrivFldName(fldRef.FieldName);
						return;
					}
					var fld = ctx.Target as CodeMemberField;
					if(null!=fld && fldMap.ContainsKey(fld.Name))
					{
						fld.Name = _ToPrivFldName(fld.Name);
						var ctr = fld.Type;
						if (0 < ctr.ArrayRank)
							ctr = ctr.ArrayElementType;
						if (!CodeDomResolver.IsPrimitiveType(ctr))
							ctr.BaseType = _ToNetCase(ctr.BaseType);
						
					}
					var prop = ctx.Target as CodeMemberProperty;
					if(null!=prop && (propElem.Contains(prop.Name) || propAttr.Contains(prop.Name)))
					{
						var n = prop.Name;
						var ctr = prop.Type;
						if (0 < ctr.ArrayRank)
							ctr = ctr.ArrayElementType;
						if (!CodeDomResolver.IsPrimitiveType(ctr))
							ctr.BaseType = _ToNetCase(ctr.BaseType);
						prop.Name = _ToNetCase(n);
						if (propElem.Contains(n))
						{
							foreach (CodeAttributeDeclaration a in prop.CustomAttributes)
							{
								if (0 == string.Compare("System.Xml.Serialization.XmlElementAttribute", a.AttributeType.BaseType, StringComparison.InvariantCulture))
								{
									a.Arguments.Insert(0, new CodeAttributeArgument("ElementName", new CodePrimitiveExpression(n)));
									break;
								}
							}
						}
						else
						{
							foreach (CodeAttributeDeclaration a in prop.CustomAttributes)
							{
								if (0 == string.Compare("System.Xml.Serialization.XmlAttributeAttribute", a.AttributeType.BaseType, StringComparison.InvariantCulture))
								{
									a.Arguments.Insert(0, new CodeAttributeArgument("AttributeName", new CodePrimitiveExpression(n)));
									break;
								}
							}
						}
					}

				});
			}
			
			// Check for invalid characters in identifiers
			CodeGenerator.ValidateIdentifiers(ns);

			// output the C# code
			Console.WriteLine(CodeDomUtility.ToString(ccu));

		}

		static string _ToNetCase(string s)
		{
			return string.Concat(char.ToUpperInvariant(s[0]).ToString(), s.Substring(1));
		}
		static string _ToCamelCase(string s)
		{
			return string.Concat(char.ToLowerInvariant(s[0]).ToString(), s.Substring(1));
		}
		static string _ToPrivFldName(string s)
		{
			return string.Concat("_",_ToCamelCase(s).Substring(0,s.Length-5));
		}

		private static void _FillPropFldMaps(CodeTypeDeclaration td, HashSet<string> propElem, HashSet<string> propAttr, Dictionary<string, string> fldMap)
		{
			CodeDomVisitor.Visit(td, (ctx) =>
			{
				var prop = ctx.Target as CodeMemberProperty;
				if (null != prop)
				{
					foreach (CodeAttributeDeclaration decl in prop.CustomAttributes)
					{
						var isAttr = 0 == string.Compare("System.Xml.Serialization.XmlAttributeAttribute", decl.AttributeType.BaseType, StringComparison.InvariantCulture);
						var isElem = !isAttr && 0 == string.Compare("System.Xml.Serialization.XmlElementAttribute", decl.AttributeType.BaseType, StringComparison.InvariantCulture);
						if (isAttr || isElem)
						{
							if (0 < prop.GetStatements.Count)
							{
								var mr = prop.GetStatements[0] as CodeMethodReturnStatement;
								if (null != mr)
								{
									var fr = mr.Expression as CodeFieldReferenceExpression;
									if (null != fr)
									{
										fldMap.Add(fr.FieldName, prop.Name);
										if (isElem)
											propElem.Add(prop.Name);
										if (isAttr)
											propAttr.Add(prop.Name);
									}
								}
							}
							break;
						}
					}
				}
			}, CodeDomVisitTargets.Types | CodeDomVisitTargets.Members);
		}

		private static void _FillTypeMap(CodeNamespace codeNamespace, Dictionary<string, CodeTypeDeclaration> typeMap)
		{
			CodeDomVisitor.Visit(codeNamespace, (ctx) =>
			{
				var td = ctx.Target as CodeTypeDeclaration;
				if (null != td)
				{
					foreach (CodeAttributeDeclaration decl in td.CustomAttributes)
					{
						if (0 == string.Compare("System.Xml.Serialization.XmlTypeAttribute", decl.AttributeType.BaseType, StringComparison.InvariantCulture))
						{
							typeMap.Add(td.Name, td);
							break;
						}
					}
				}
			}, CodeDomVisitTargets.Types);
		}
	}
}