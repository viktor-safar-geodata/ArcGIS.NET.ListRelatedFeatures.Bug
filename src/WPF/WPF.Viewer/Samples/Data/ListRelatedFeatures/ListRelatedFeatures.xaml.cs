// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace ArcGIS.WPF.Samples.ListRelatedFeatures
{
    [ArcGIS.Samples.Shared.Attributes.Sample(
        name: "List related features",
        category: "Data",
        description: "List features related to the selected feature.",
        instructions: "Click on a feature to select it. The related features will be displayed in a list.",
        tags: new[] { "features", "identify", "query", "related", "relationship", "search" }
    )]
    public partial class ListRelatedFeatures
    {
        // Hold the URL of the web map.
        private readonly Uri _mapUri = new Uri(
            "https://arcgisruntime.maps.arcgis.com/home/item.html?id=dcc7466a91294c0ab8f7a094430ab437"
        );

        // Reference to the feature layer.
        private FeatureLayer _OverettlinjeLysFeatureLayer;

        public ListRelatedFeatures()
        {
            InitializeComponent();

            // Create the UI, setup the control references and execute initialization.
            _ = Initialize();
        }

        private async Task Initialize()
        {
            try
            {
                // Create the map from the portal item.
                Map myMap = new Map(SpatialReferences.WebMercator)
                {
                    InitialViewpoint = new Viewpoint(
                        new Envelope(-180, -85, 180, 85, SpatialReferences.Wgs84)
                    ),
                    Basemap = new Basemap(BasemapStyle.ArcGISStreets)
                };

                // Add the map to the mapview.
                MyMapView.Map = myMap;

                await InitializeGeodatabase(myMap);

                // Wait for the map to load.
                //await myMap.LoadAsync();

                // Get the feature layer from the map.
                _OverettlinjeLysFeatureLayer = (FeatureLayer)
                    myMap.OperationalLayers.FirstOrDefault(x => x.Name == "OverettlinjeLys");

                // Wait for the layer to load.
                await _OverettlinjeLysFeatureLayer.LoadAsync();

                await MyMapView.SetViewpointAsync(
                    new Viewpoint(_OverettlinjeLysFeatureLayer.FullExtent)
                );

                // Make the selection color yellow.
                MyMapView.SelectionProperties.Color = Color.Yellow;

                // Listen for GeoViewTapped events.
                MyMapView.GeoViewTapped += MyMapViewOnGeoViewTapped;

                // Hide the loading indicator.
                LoadingProgress.Visibility = Visibility.Collapsed;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error");
            }
        }

        private string Light1GlobalId = "{F9158A1D-A1BF-4110-B964-6B25ABC0E143}";

        private async void MyMapViewOnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // Clear any existing feature selection and results list.
            _OverettlinjeLysFeatureLayer.ClearSelection();
            MyResultsView.ItemsSource = null;

            try
            {
                // I do not care where you clicked
                // I am just gonna find a feature of interest

                var results = await _OverettlinjeLysFeatureLayer.FeatureTable.QueryFeaturesAsync(
                    new QueryParameters() { WhereClause = $"FKNavinst1 = '{Light1GlobalId}'" }
                );

                // Get the first result.
                ArcGISFeature myFeature = (ArcGISFeature)results.First();

                // Select the feature.
                _OverettlinjeLysFeatureLayer.SelectFeature(myFeature);

                // Get the feature table for the feature.
                ArcGISFeatureTable myFeatureTable = (ArcGISFeatureTable)myFeature.FeatureTable;

                // Query related features.
                IReadOnlyList<RelatedFeatureQueryResult> relatedFeaturesResult =
                    await myFeatureTable.QueryRelatedFeaturesAsync(myFeature);

                // Create a list to hold the formatted results of the query.
                List<string> queryResultsForUi = new List<string>();

                var globalId = myFeature.Attributes["globalid"];
                var fk1 = myFeature.Attributes["FKNavinst1"];
                var fk2 = myFeature.Attributes["FKNavinst2"];

                queryResultsForUi.Add($"Feature OverettlinjeLys with globalid");
                queryResultsForUi.Add($"{globalId}");
                queryResultsForUi.Add("");
                queryResultsForUi.Add("has 2 FKs to related features:");
                queryResultsForUi.Add($"FK1: {fk1}");
                queryResultsForUi.Add($"FK2: {fk2}");
                queryResultsForUi.Add("");
                queryResultsForUi.Add(
                    "But features returned from QueryRelatedFeaturesAsync are the same:"
                );

                // For each query result.
                foreach (RelatedFeatureQueryResult result in relatedFeaturesResult)
                {
                    // And then for each feature in the result.
                    foreach (Feature resultFeature in result)
                    {
                        // Get a reference to the feature's table.
                        ArcGISFeatureTable relatedTable = (ArcGISFeatureTable)
                            resultFeature.FeatureTable;

                        // Get the display field name - this is the name of the field that is intended for display.
                        string displayFieldName = relatedTable.LayerInfo.DisplayFieldName;

                        // Get the name of the feature's table.
                        string tableName = relatedTable.TableName;

                        // Get the display name for the feature.
                        string featureDisplayname = resultFeature
                            .Attributes[displayFieldName]
                            .ToString();

                        var featureGlobalId = resultFeature.Attributes["globalid"].ToString();

                        // Create a formatted result string.
                        string formattedResult = string.Format(
                            "{0} - {1} - {2}",
                            tableName,
                            featureDisplayname,
                            featureGlobalId
                        );

                        // Add the result to the list.
                        queryResultsForUi.Add(formattedResult);
                    }
                }

                // Update the UI with the result list.
                MyResultsView.ItemsSource = queryResultsForUi;

                // Hide the loading indicator.
                LoadingProgress.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private async Task InitializeGeodatabase(Map map)
        {
            var gdb = await GetGeodatabase();
            await LoadAllTablesToMap(map, gdb);
        }

        private async Task LoadAllTablesToMap(Map map, Geodatabase gdb)
        {
            foreach (var table in gdb.GeodatabaseFeatureTables)
            {
                await table.LoadAsync();
                var layer = new FeatureLayer(table);
                await layer.LoadAsync();
                layer.IsVisible = layer.Name == "OverettlinjeLys" || layer.Name == "Lys";
                map.OperationalLayers.Add(layer);
                Debug.WriteLine($"Added layer {layer.Name} in the map");
            }
        }

        private async Task<Geodatabase> GetGeodatabase()
        {
            var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var gdbPath = Path.Combine(
                executingPath.Replace("WPF.Viewer\\bin\\Debug\\net8.0-windows10.0.19041.0", ""),
                "data",
                "gdb.geodatabase"
            );
            return await Geodatabase.OpenAsync(gdbPath);
        }
    }
}
