using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubPointSolutions.CakeBuildTools.Data;

namespace SubPointSolutions.CakeBuildTools.Services
{
    public static class SampleReadAPI
    {
        #region properties


        #endregion

        #region methods

        public static IEnumerable<DocSample> LoadSamples(string samplesFolder)
        {
            var allSamples = new List<DocSample>();

            if (System.IO.Directory.Exists(samplesFolder))
            {
                var allSampleFilePaths = System.IO.Directory.GetFiles(samplesFolder, "*.smpindx", System.IO.SearchOption.AllDirectories);

                foreach (var allSampleFilePath in allSampleFilePaths)
                {
                    var sampleXmlContent = System.IO.File.ReadAllText(allSampleFilePath);
                    var sampleInstance = DocSample.FromXml(sampleXmlContent);

                    allSamples.Add(sampleInstance);
                }
            }

            return allSamples;
        }

        public static string GetSafeAnchor(string value)
        {
            return value.ToLower()
                        .Replace(" ", "-");
        }

        public static List<DocSample> GetSamplesWithTag(IEnumerable<DocSample> samples, string tagName)
        {
            return GetSamplesWithTag(samples, tagName, null);
        }

        public static List<DocSample> GetSamplesWithTag(IEnumerable<DocSample> samples, string tagName, string tagValue)
        {
            var categoryTagName = tagName;

            if (tagValue != null)
            {
                return samples.Where(s => s.Tags.Any(t => t.Name == categoryTagName && t.Values.Contains(tagValue)))
                       .ToList();
            }

            return samples.Where(s => s.Tags.Any(t => t.Name == categoryTagName && t.Values.Any()))
                          .ToList();
        }

        public static List<DocSample> GetSamplesWithCategories(IEnumerable<DocSample> samples)
        {
            return GetSamplesWithTag(samples, BuiltInTagNames.Category, null);
        }

        public static bool HasTag(DocSample sample, string tagName)
        {
            var tag = sample.Tags.FirstOrDefault(t => t.Name == tagName);

            return tag != null;
        }

        public static bool HasTagValue(DocSample sample, string tagName, string value)
        {
            var tag = sample.Tags.FirstOrDefault(t => t.Name == tagName);

            if (tag == null)
                return false;

            if (!tag.Values.Any())
                return false;

            return tag.Values.Contains(value);
        }


        public static string GetSampleTagValue(DocSample sample, string tagName, string defaultValue)
        {
            var tag = sample.Tags.FirstOrDefault(t => t.Name == tagName);

            if (tag != null && tag.Values.Any())
            {
                return tag.Values.First();
            }

            return defaultValue;
        }

        public static List<string> GetSampleTagValues(DocSample sample, string tagName, List<string> defaultValues)
        {
            var tag = sample.Tags.FirstOrDefault(t => t.Name == tagName);

            if (tag != null && tag.Values.Any())
            {
                return tag.Values;
            }

            return defaultValues;
        }

        public static List<string> GetSampleTagValues(IEnumerable<DocSample> samples, string tagName)
        {
            var result = new List<string>();

            foreach (var sample in samples)
            {
                result.AddRange(GetSampleTagValues(sample, tagName, new List<string>()));
            }

            return result.Distinct().ToList();
        }

        public static List<string> GetSampleCategories(IEnumerable<DocSample> samples)
        {
            var categoryTagName = BuiltInTagNames.Category;

            var samplesWithCategories = samples.Where(s =>
                                                  s.Tags.Any(t => t.Name == categoryTagName && t.Values.Any()))
                                                  .ToList();


            var sampleCategories = samplesWithCategories
                             .SelectMany(s => s.Tags.First(t => t.Name == categoryTagName).Values.Distinct())
                             .Distinct()
                             .ToList();

            return sampleCategories;
        }

        #endregion
    }
}
