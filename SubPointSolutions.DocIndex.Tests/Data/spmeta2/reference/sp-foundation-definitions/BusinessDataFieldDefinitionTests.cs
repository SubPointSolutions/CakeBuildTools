using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPMeta2.Docs.ProvisionSamples.Base;
using SPMeta2.Docs.ProvisionSamples.Definitions;
using SPMeta2.Syntax.Default;
using SubPointSolutions.Docs.Code.Enumerations;
using SubPointSolutions.Docs.Code.Metadata;

namespace SPMeta2.Docs.ProvisionSamples.Provision.Definitions
{
    [TestClass]

    [SampleMetadataTag(Name = BuiltInTagNames.SPRuntime, Value = BuiltInSPRuntimeTagValues.Foundation)]

    [SampleMetadataTag(Name = BuiltInTagNames.SampleCategory, Value = BuiltInSampleCategoryTagValues.Fields)]
    [SampleMetadataTag(Name = BuiltInTagNames.SampleM2Model, Value = BuiltInM2ModelTagValues.SiteModel)]

    //[SampleMetadataTag(Name = BuiltInTagNames.SampleHidden)]
    public class BusinessDataFieldDefinitionTests : ProvisionTestBase
    {
        #region methods

        [TestMethod]
        [TestCategory("Docs.BusinessDataFieldDefinition")]

        [SampleMetadata(Title = "Add business data field",
                        Description = ""
                        )]
        [SampleMetadataTag(Name = BuiltInTagNames.SampleHidden)]
        public void CanDeploySimpleBusinessDataFieldDefinition()
        {
            var model = SPMeta2Model.NewSiteModel(site =>
            {

            });

            DeployModel(model);
        }

        #endregion
    }
}