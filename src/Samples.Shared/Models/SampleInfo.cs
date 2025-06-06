﻿// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGIS.Samples.Managers;
using ArcGIS.Samples.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using static System.Net.WebRequestMethods;

namespace ArcGIS.Samples.Shared.Models
{
    public partial class SampleInfo
    {
#if NETFX_CORE
        private string _pathStub = Windows.ApplicationModel.Package.Current.Installed­Location.Path;
#else
        private string _pathStub = System.IO.Path.GetDirectoryName(AppContext.BaseDirectory);
#endif

        /// <summary>
        /// Gets the path to the sample on disk.
        /// </summary>
        public string Path
        {
            get
            {
                string formalCategory = Category;
                if (formalCategory.Contains(' '))
                {
                    formalCategory = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Category).Replace(" ", "");
                }
                return System.IO.Path.Combine(_pathStub, "Samples", formalCategory, FormalName);
            }
        }

        /// <summary>
        /// The human-readable name of the sample.
        /// </summary>
        public string SampleName { get; set; }

        /// <summary>
        /// The name of the sample as it appears in code.
        /// </summary>
        public string FormalName { get; set; }

        /// <summary>
        /// The human-readable category of the sample.
        /// </summary>
        public string Category { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }


#if !(WinUI || WINDOWS_UWP)
        public bool IsFavorite { get; set; }

        public bool ShowFavoriteIcon
        {
            get
            {
                // Determine if on mobile.
                bool isMobile;
#if ANDROID || IOS
                isMobile = true;
#else
                isMobile = false;
#endif
                // Always show icons on mobile platforms; on desktop, only show yellow stars.
                return isMobile || SampleManager.Current.IsSampleFavorited(FormalName);
            }
        }
#endif

        /// <summary>
        /// A list of offline data items that should be downloaded
        /// from ArcGIS Online prior to loading the sample. These
        /// should be expressed as portal item identifiers.
        /// </summary>
        public IEnumerable<string> OfflineDataItems { get; set; }

        public IEnumerable<string> Tags { get; set; }

        public IEnumerable<string> AndroidLayouts { get; set; }

        public IEnumerable<string> XamlLayouts { get; set; }

        public IEnumerable<string> ClassFiles { get; set; }

        /// <summary>
        /// A list of files used by the sample as embedded resources (e.g. PictureMarkerSymbols\pin_star_blue.png)
        /// </summary>
        public IEnumerable<string> EmbeddedResources { get; set; }

        /// <summary>
        /// The expected filename of the sample's image, without path.
        /// This is intened for use on Windows.
        /// </summary>
        public string Image
        {
            get
            {
                return String.Format("{0}.jpg", FormalName);
            }
        }

        /// <summary>
        /// The underlying .NET type for this sample.
        /// Note: this is used by the sample viewer to
        /// construct samples at run time.
        /// </summary>
        public Type SampleType { get; set; }

        /// <summary>
        /// The path to the sample image on disk; intended for use on Windows.
        /// </summary>
        public string SampleImageName
#if MAUI
#if __IOS__
            => Image.ToLower();
#else
            => $"Samples/{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Category).Replace(" ","")}/{FormalName}/{Image.ToLower()}";
#endif
#else
            => System.IO.Path.Combine(Path, Image);
#endif

        /// <summary>
        /// Base directory for the samples; defaults to executable directory
        /// </summary>
        public string PathStub
        {
            get { return _pathStub; }
            set { _pathStub = value; }
        }

        /// <summary>
        /// This constructor is for use when the sample
        /// type is in the executing assembly.
        /// </summary>
        /// <param name="sampleType">The type for the sample object.</param>
        public SampleInfo(Type sampleType)
        {
            SampleType = sampleType;
            FormalName = SampleType.Name;
            TypeInfo typeInfo = sampleType.GetTypeInfo();

            var sampleAttr = GetAttribute<SampleAttribute>(typeInfo);
            if (sampleAttr == null) { throw new ArgumentException("Type must be decorated with 'Sample' attribute"); }

            var offlineDataAttr = GetAttribute<OfflineDataAttribute>(typeInfo);
            var xamlAttr = GetAttribute<XamlFilesAttribute>(typeInfo);
            var androidAttr = GetAttribute<AndroidLayoutAttribute>(typeInfo);
            var classAttr = GetAttribute<ClassFileAttribute>(typeInfo);
            var embeddedResourceAttr = GetAttribute<EmbeddedResourceAttribute>(typeInfo);

            Category = sampleAttr.Category;
            Description = sampleAttr.Description;
            Instructions = sampleAttr.Instructions;
            SampleName = sampleAttr.Name;
            Tags = sampleAttr.Tags;
            if (androidAttr != null) { AndroidLayouts = androidAttr.Files; }
            if (xamlAttr != null) { XamlLayouts = xamlAttr.Files; }
            if (classAttr != null) { ClassFiles = classAttr.Files; }
            if (offlineDataAttr != null) { OfflineDataItems = offlineDataAttr.Items; }
            if (embeddedResourceAttr != null) { EmbeddedResources = embeddedResourceAttr.Files; }
        }

        /// <summary>
        /// Gets the attribute of type <typeparamref name="T"/> for a type described by <paramref name="typeInfo"/>.
        /// </summary>
        /// <typeparam name="T">The type of the attribute object to return.</typeparam>
        /// <param name="typeInfo">Describes the type that will be examined.</param>
        /// <returns>The matching attribute object.</returns>
        private static T GetAttribute<T>(MemberInfo typeInfo) where T : Attribute
        {
            return typeInfo.GetCustomAttributes(typeof(T)).SingleOrDefault() as T;
        }

#if !WINDOWS_UWP
        /// <summary>
        /// Get the GitHub url for a given sample folder.
        /// </summary>
        /// <returns>The full GitHub url.</returns>
        public string GetGitHubUrl()
        {
            var repoUrl = "https://github.com/Esri/arcgis-maps-sdk-dotnet-samples";
            string samplesPath;
            string fullPath;

            samplesPath = Path.Substring(Path.LastIndexOf("Samples")).Replace("\\", "/");
            fullPath = repoUrl + "/tree/main/src/WPF/WPF.Viewer/" + samplesPath;

#if NETFRAMEWORK
            samplesPath = Path.Substring(Path.LastIndexOf("Samples")).Replace("//", "\\");
            fullPath = repoUrl + "/tree/main/src/WPF/WPF.Viewer/" + samplesPath;
#elif WinUI
            samplesPath = Path.Substring(Path.LastIndexOf("Samples")).Replace("\\", "/");
            fullPath = repoUrl + "/tree/main/src/WinUI/ArcGIS.WinUI.Viewer/" + samplesPath;
#elif MAUI
            samplesPath = Path.Substring(Path.LastIndexOf("Samples")).Replace("\\", "/");
            fullPath = repoUrl + "/tree/main/src/MAUI/Maui.Samples/" + samplesPath;
#endif
            return fullPath;
        }
#endif
    }
}