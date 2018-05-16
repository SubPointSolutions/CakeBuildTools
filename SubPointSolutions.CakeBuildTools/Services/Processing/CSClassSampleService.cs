using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SubPointSolutions.CakeBuildTools.Data;
using SubPointSolutions.CakeBuildTools.Utils;

namespace SubPointSolutions.CakeBuildTools.Services.Processing
{
    public class CSClassSamplesService : CSMethodSampleService
    {
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

            Console.WriteLine(String.Format("Found classes: {0}", csClasses.Count));

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

                var namespaceName = string.Empty;

                if (csClass.Parent != null && csClass.Parent is NamespaceDeclarationSyntax)
                {
                    namespaceName = (csClass.Parent as NamespaceDeclarationSyntax).Name.ToString();
                }

                var sample = new DocSample();

                sample.IsClass = true;
                sample.IsMethod = false;

                // namespace
                sample.Namespace = namespaceName;
                sample.Language = "cs";

                // class level
                sample.ClassName = className;
                sample.ClassFullName = namespaceName + "." + className;

                sample.ClassComment = classComment;


                var classBody = "    " + csClass.ToString();

                // cleaning up attributes
                foreach (var classAttr in csClass.AttributeLists.ToList())
                {
                    classBody = classBody.Replace(classAttr.ToString(), string.Empty);
                }

                // method
                sample.MethodBodyWithFunction = classBody;
                sample.MethodBody = classBody;

                sample.MethodName = className + "Class";
                sample.MethodFullName = "Class" + sample.MethodName;

                sample.SourceFileName = Path.GetFileName(filePath);
                sample.SourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                sample.SourceFileFolder = Path.GetDirectoryName(filePath);
                sample.SourceFilePath = filePath;

                // TODO, metadata
                sample.Title = string.Empty;
                sample.Description = string.Empty;

                var classAttributes = csClass.AttributeLists;

                var sampleTitleAttrValue = GetAttributeValue(classAttributes, "DisplayName");
                var sampleDescriptionAttrValue = GetAttributeValue(classAttributes, "Description");
                var sampleCategoryAttrValue = GetAttributeValue(classAttributes, "Category");
                var sampleVisibilityAttrValue = GetAttributeValue(classAttributes, "Browsable");

                if (!string.IsNullOrEmpty(sampleTitleAttrValue))
                    sample.Title = sampleTitleAttrValue;

                if (!string.IsNullOrEmpty(sampleDescriptionAttrValue))
                    sample.Description = sampleDescriptionAttrValue;

                FillSampleTags(sample, sampleCategoryAttrValue);

                var isSampleHidden = false;

                if (!string.IsNullOrEmpty(sampleVisibilityAttrValue))
                {
                    var isVisible = ConvertUtils.ToBool(sampleVisibilityAttrValue);

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

                result.Add(sample);
                //}
            }


            return result;
        }
        #endregion
    }
}
