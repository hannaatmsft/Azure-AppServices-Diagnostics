﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class ResiliencyReportData : IResiliencyReportData
    {
        [JsonConverter(typeof(ResiliencyResource))]
        ResiliencyResource[] resiliencyResourceList;

        /// <summary>
        /// Creates an instance of ResiliencyReportData
        /// </summary>
        public ResiliencyReportData(string customerName, ResiliencyResource[] resiliencyResourceList)
        {
            this.CustomerName = customerName;
            if (resiliencyResourceList == null)
            {
                throw new ArgumentNullException(nameof(resiliencyResourceList), $"{nameof(resiliencyResourceList)} cannot be null");
            }
            else
            {
                this.resiliencyResourceList = resiliencyResourceList;
            }
        }
        /// <summary>
        /// Customer's name used for the report's cover. This will normally be either customer's Company or simply Customer's name obtained from the subscription.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// Array containing a list of all the resources to be included in the report along with their 
        /// </summary>


        public ResiliencyResource[] GetResiliencyResourceList()
        {
            return this.resiliencyResourceList;
        }

        public void SetResiliencyResourceList(ResiliencyResource[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), $"{nameof(value)} cannot be null");
            }
            else
            {
                this.resiliencyResourceList = value;
            }

        }


    }

    /// <summary>
    /// ResiliencyResources supported can be Web App or ASE
    /// </summary>
    public class ResiliencyResource : IResiliencyResource
    {
        double _overallScore;
        ResiliencyFeature[] resiliencyFeaturesList;
        IDictionary<string, Weight> _featuresDictionary;

        /// <summary>
        /// Constructor
        /// </summary>
        public ResiliencyResource(string name, IDictionary<string, Weight> featuresDictionary)
        {
            this.Name = name;
            _overallScore = 0;
            if (featuresDictionary != null)
            {
                _featuresDictionary = featuresDictionary;
                this.resiliencyFeaturesList = new ResiliencyFeature[_featuresDictionary.Count];
                ResiliencyFeature[] resiliencyFeaturesList = new ResiliencyFeature[_featuresDictionary.Count];

                var i = 0;
                foreach (KeyValuePair<string, Weight> kvp in _featuresDictionary)
                {
                    ResiliencyFeature feature = new ResiliencyFeature(kvp.Key, kvp.Value);
                    resiliencyFeaturesList[i] = feature;
                    i++;
                }
                this.resiliencyFeaturesList = resiliencyFeaturesList;
            }
            else
            {
                throw new ArgumentNullException(nameof(featuresDictionary), $"{nameof(featuresDictionary)} cannot be null");
            }
        }

        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Overall Score for the Resource is calculated based on the grade of each feature.
        /// </summary>
        public double CalculateOverallScore()
        {
            _overallScore = 0;
            double _overallScoreSum = 0;
            double _featureWeightsSum = 0;
            foreach (ResiliencyFeature rf in resiliencyFeaturesList)
            {
                Weight _weight;
                Grade _grade;
                bool weightParsingResult = Enum.TryParse<Weight>(rf.FeatureWeight.ToString(), out _weight);
                bool gradeParsingResult = Enum.TryParse<Grade>(rf.ImplementationGrade.ToString(), out _grade);
                if (weightParsingResult && gradeParsingResult)
                {
                    _overallScoreSum += (int)rf.FeatureWeight * (int)rf.ImplementationGrade;
                    _featureWeightsSum += (int)rf.FeatureWeight;
                }
            }
            _overallScore = Math.Round((_overallScoreSum * 100) / (_featureWeightsSum * 2), 1);
            return Math.Round(_overallScore, 2);
        }

        public ResiliencyFeature[] GetResiliencyFeaturesList()
        {
            return this.resiliencyFeaturesList;
        }

        public void SetResiliencyFeaturesList(ResiliencyFeature[] value)
        {
            this.resiliencyFeaturesList = value;
        }

        /// <summary>
        /// Call CalculatedOverallScore to recalculate the OverallScore to make sure it calculates with latest changes.
        /// </summary>
        public double OverallScore { get => CalculateOverallScore(); }


    }

    /// <summary>
    /// ResiliencyFeature is the object that will contain the data for the check done on a particular detector
    /// </summary>
    public class ResiliencyFeature : IResiliencyFeature
    {
        /// <summary>
        /// Creates an instance of ResiliencyFeature class.
        /// </summary>
        /// <param name="name">Name of the Resiliency Feature evaluated.</param>
        /// <param name="featureWeight">Enum representing the Feature Weight.</param>
        public ResiliencyFeature(string name, Weight featureWeight)
        {
            Name = name;
            FeatureWeight = featureWeight;
            ImplementationGrade = 0;
            GradeComments = "";
            SolutionComments = "";
        }

        /// <summary>
        /// Name of the Resiliency Feature evaluated.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Feature Weight explanation:
        /// Mandatory: 25 - Without this feature implemented, the app will most likely have availability issues.
        /// Important (ASE): 15 - Allows for reliability in special situations (for example Multiple zones/Regions, Regional pairing, etc.)
        /// Important: 5  - Allows for reliability in special situations (for example Multiple zones/Regions, Regional pairing, etc.)
        /// GoodToHave: 1
        /// Notcalculated): 0
        /// </summary>
        public Weight FeatureWeight { get; set; }

        /// <summary>
        /// Enum representing the Grade obtained in this feature:
        /// 
        /// </summary>
        public Grade ImplementationGrade { get; set; }

        /// <summary>
        /// Comments explaining the grade obtained by this site.
        /// </summary>
        public string GradeComments { get; set; }

        /// <summary>
        /// Comments to be included with the solution in case of a failing grade
        /// </summary>
        public string SolutionComments { get; set; }
    }

    public static class ResponseResiliencyReportExtension
    {
        public static DiagnosticData AddResiliencyReportData(this Response response, ResiliencyReportData resiliencyReportData)
        {
            if (response == null || resiliencyReportData == null)
            {
                return null;
            }
            var table = new DataTable();
            table.Columns.Add(new DataColumn("ResiliencyReport", typeof(string)));
            table.Columns.Add(new DataColumn("ResiliencyResourceList", typeof(string)));
            table.Columns.Add(new DataColumn("ResiliencyFeaturesList", typeof(string)));
            string jsonResiliencyReport = JsonConvert.SerializeObject(resiliencyReportData, Formatting.Indented);
            string jsonResiliencyResourceList = JsonConvert.SerializeObject(resiliencyReportData.GetResiliencyResourceList(), Formatting.Indented);
            string jsonResiliencyFeaturesList;
            for (int siteNum = 0; siteNum < resiliencyReportData.GetResiliencyResourceList().GetLength(0); siteNum++)
            {
                jsonResiliencyFeaturesList = JsonConvert.SerializeObject(resiliencyReportData.GetResiliencyResourceList()[siteNum].GetResiliencyFeaturesList(), Formatting.Indented);
                table.Rows.Add(new object[] { jsonResiliencyReport, jsonResiliencyResourceList, jsonResiliencyFeaturesList });
            }

            var diagnosticData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Report)
            };
            response.Dataset.Add(diagnosticData);
            return diagnosticData;
        }
    }
}
