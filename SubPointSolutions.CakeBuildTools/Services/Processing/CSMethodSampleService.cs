using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SubPointSolutions.CakeBuildTools.Data;
using SubPointSolutions.CakeBuildTools.Utils;

namespace SubPointSolutions.CakeBuildTools.Services.Processing
{
    public class CSMethodSampleService : SamplesServiceBase
    {
        #region constructors

        public CSMethodSampleService()
        {
            FileExtension = "*.cs";
        }

        #endregion

        #region methods
        public override IEnumerable<DocSample> CreateSamplesFromSourceFile(string filePath)
        {
            var result = new List<DocSample>();

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            var treeRoot = tree.GetRoot() as CompilationUnitSyntax;

            var csClasses = tree.GetRoot()
                                .DescendantNodes()
                                .OfType<ClassDeclarationSyntax>()
                                .ToList();

            foreach (var csClass in csClasses)
            {
                var className = csClass.Identifier.ToString();
                var classComment = string.Empty;

                var trivia = csClass.GetLeadingTrivia();

                if (trivia != null)
                {
                    var commentXml = trivia.ToString();

                    try
                    {
                        classComment = XElement.Parse(trivia.ToString()
                                                            .Replace(@"///", string.Empty)
                                                            .Trim())
                                          .FirstNode
                                          .ToString()
                                          .Trim()
                                          .Replace("     ", "");
                    }
                    catch (Exception)
                    {

                    }
                }

                // methods
                var csMethods = csClass
                                  .DescendantNodes()
                                  .OfType<MethodDeclarationSyntax>().ToList();

                Console.WriteLine(String.Format("Found methods: {0}", csMethods.Count));

                foreach (var csMethod in csMethods)
                {
                    Console.WriteLine(String.Format("Parsing method: {0}", csMethod));

                    var namespaceName = ((csMethod.Parent as ClassDeclarationSyntax).Parent as NamespaceDeclarationSyntax).Name.ToString();

                    var methodName = csMethod.Identifier.Text;
                    var methodFullName = namespaceName + "." + className + "." + methodName;

                    // cleaning up attributes
                    var methodBodyWithFuntion = csMethod.ToString();

                    foreach (var methodAttr in csMethod.AttributeLists.ToList())
                    {
                        methodBodyWithFuntion = methodBodyWithFuntion.Replace(methodAttr.ToString(), string.Empty);
                    }

                    var sample = new DocSample();

                    // namespace
                    sample.Namespace = namespaceName;
                    sample.Language = "cs";

                    // class level
                    sample.ClassName = className;
                    sample.ClassFullName = namespaceName + "." + className;

                    sample.ClassComment = classComment;

                    // method
                    sample.MethodBodyWithFunction = methodBodyWithFuntion;
                    sample.MethodName = methodName;
                    sample.MethodFullName = methodFullName;

                    sample.MethodParametersCount = csMethod.ParameterList.ChildNodes().Count();

                    var hasOverload = csMethods.Count(m => m.Identifier.Text == methodName) > 1;

                    if (hasOverload)
                    {
                        sample.MethodName = methodName + "_" + sample.MethodParametersCount + "_Params";
                    }

                    // abstract methods don't have any body
                    if (csMethod.Body != null)
                    {
                        sample.MethodBody = csMethod.Body
                            .ToString()
                            .TrimStart('{')
                            .TrimEnd('}');
                    }
                    

                    sample.SourceFileName = Path.GetFileName(filePath);
                    sample.SourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    sample.SourceFileFolder = Path.GetDirectoryName(filePath);
                    sample.SourceFilePath = filePath;

                    // extracting sample metadata
                    // [System.ComponentModel.DisplayName("Sample name")]
                    // [System.ComponentModel.Description("Sample decription")]

                    // [System.ComponentModel.Category("Web Model.Security")]
                    // [System.ComponentModel.Browsable(true)]


                    // load from SampleMetadata attr
                    var instanceType = Type.GetType(sample.ClassFullName);

                    var methodAttributes = csMethod.AttributeLists;
                    var classAttributes = (csMethod.Parent as ClassDeclarationSyntax).AttributeLists;

                    var sampleTitleAttrValue = GetAttributeValue(methodAttributes, "DisplayName");
                    var sampleDescriptionAttrValue = GetAttributeValue(methodAttributes, "Description");
                    var sampleCategoryAttrValue = GetAttributeValue(methodAttributes, "Category");
                    var sampleVisibilityAttrValue = GetAttributeValue(classAttributes, "Browsable");

                    var classCategoryAttrValue = GetAttributeValue(classAttributes, "Category");
                    var classVisibilityAttrValue = GetAttributeValue(classAttributes, "Browsable");

                    if (!string.IsNullOrEmpty(sampleTitleAttrValue))
                        sample.Title = sampleTitleAttrValue;

                    if (!string.IsNullOrEmpty(sampleDescriptionAttrValue))
                        sample.Description = sampleDescriptionAttrValue;

                    FillSampleTags(sample, classCategoryAttrValue);
                    FillSampleTags(sample, classCategoryAttrValue);

                    var isSampleHidden = false;

                    if (!string.IsNullOrEmpty(sampleVisibilityAttrValue))
                    {
                        var isVisible = ConvertUtils.ToBool(sampleVisibilityAttrValue);

                        if (isVisible.HasValue && isVisible.Value == true)
                            isSampleHidden = true;
                    }

                    if (!string.IsNullOrEmpty(classVisibilityAttrValue))
                    {
                        var isVisible = ConvertUtils.ToBool(classVisibilityAttrValue);

                        if (isVisible.HasValue && isVisible.Value == true)
                            isSampleHidden = true;
                    }

                    if (isSampleHidden)
                    {
                        var hiddenTag = sample.Tags.FirstOrDefault(t => t.Name == "Hidden");

                        if (hiddenTag == null)
                        {
                            hiddenTag = new DocSampleTag { Name = "Hidden" };
                            sample.Tags.Add(hiddenTag);
                        }
                    }

                    //var method = instanceType.GetMethods().FirstOrDefault(m => m.Name == methodName);

                    //if (method != null)
                    //{
                    //    var methodMetadata = method.GetCustomAttributes(typeof(SampleMetadataAttribute), false)
                    //                               .FirstOrDefault() as SampleMetadataAttribute;

                    //    if (methodMetadata != null)
                    //    {
                    //        sample.Title = methodMetadata.Title;
                    //        sample.Description = methodMetadata.Description;
                    //    }
                    //    else
                    //    {
                    //        // fallbak on the method name
                    //        sample.Title = method.Name;
                    //    }

                    //    // tags


                    //    var sampleTags = (method.GetCustomAttributes(typeof(SampleMetadataTagAttribute), false)
                    //                           as SampleMetadataTagAttribute[]).ToList();


                    //    // addint top-class tags
                    //    sampleTags.AddRange(instanceType.GetCustomAttributes(typeof(SampleMetadataTagAttribute), false)
                    //                           as SampleMetadataTagAttribute[]);


                    //    foreach (var tagNames in sampleTags.GroupBy(tag => tag.Name))
                    //    {
                    //        var newTag = new DocSampleTag
                    //        {
                    //            Name = tagNames.Key,
                    //            Values = tagNames.Select(t => t.Value).ToList()
                    //        };

                    //        sample.Tags.Add(newTag);
                    //    }
                    //}

                    //// use full body?
                    //if (SamplesAPI.HasTag(sample, BuiltInTagNames.UseFullMethodBody))
                    //{
                    //    sample.MethodBody = sample.MethodBodyWithFunction;
                    //}


                    result.Add(sample);
                }
            }


            return result;
        }

        protected void FillSampleTags(DocSample sample, string sampleTagValue)
        {
            var tags = sample.Tags;

            if (!string.IsNullOrEmpty(sampleTagValue))
            {
                sampleTagValue = sampleTagValue.Trim('"');

                var tagValues = sampleTagValue.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var categoryValue in tagValues)
                {
                    var tagPartValues = categoryValue.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                    var tagName = tagPartValues[0].Trim();
                    var tagValue = tagPartValues[1].Trim();

                    var tag = tags.FirstOrDefault(t => t.Name == tagName);

                    if (tag == null)
                    {
                        tag = new DocSampleTag
                        {
                            Name = tagName,
                            Values = new List<string>()
                        };

                        tags.Add(tag);
                    }

                    var tagStringValues = new List<string>();

                    if (tagValue.Contains(','))
                    {
                        tagStringValues.AddRange(tagValue.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    else
                    {
                        tagStringValues.Add(tagValue);
                    }

                    foreach (var tagStringValue in tagStringValues)
                    {
                        if (!tag.Values.Contains(tagStringValue))
                            tag.Values.Add(tagStringValue);
                    }
                }
            }
        }

        protected string GetAttributeValue(SyntaxList<AttributeListSyntax> attributes, Type attrType)
        {
            return GetAttributeValue(attributes, attrType.Name);
        }

        protected string GetAttributeValue(SyntaxList<AttributeListSyntax> attributes, string name)
        {
            var result = string.Empty;

            AttributeSyntax attributeObj = null;

            foreach (var attr in attributes)
            {

                var childNodes = attr.ChildNodes().OfType<AttributeSyntax>();
                var targetArrt = childNodes.FirstOrDefault(s => s.Name.ToFullString().Contains(name));

                if (targetArrt != null)
                {
                    attributeObj = targetArrt;
                    break;
                }
            }

            if (attributeObj != null)
            {
                if (attributeObj.ArgumentList != null && attributeObj.ArgumentList.ChildNodes().Count() > 0)
                {
                    var firstParam = attributeObj.ArgumentList.ChildNodes().First() as AttributeArgumentSyntax;
                    result = firstParam.GetText().ToString();
                }
            }

            return result;
        }

        #endregion
    }
}
