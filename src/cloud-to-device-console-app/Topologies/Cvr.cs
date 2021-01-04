using System;
using System.Collections.Generic;
using Azure.Media.Analytics.Edge.Models;

namespace C2D_Console.Topologies
{
    /// <summary>
    /// Required modules:
    ///     1. Live Video Analytics
    ///     2. RTSP Simulator
    /// </summary>
    public class Cvr : ITopology
    {
        /// <summary>
        /// Continuous Video Recording Topology ingredients
        ///    1. Parameters: rtspUserName, rtspPassword, rtspUrl
        ///    2. Sources: `MediaGraphRtspSource`
        ///    3. Sinks: `MediaGraphAssetSink`
        ///
        /// </summary>
        ///
        /// <remark>
        /// For additional info on Media Graph and its pieces, please refer to https://docs.microsoft.com/en-us/azure/media-services/live-video-analytics-edge/media-graph-concept
        /// </remark>
        public MediaGraphTopology Build()
        {
            var graphProperties = new MediaGraphTopologyProperties
            {
                Description = "Continuous video recording to an Azure Media Services Asset",
            };

            SetParameters(graphProperties);
            SetSources(graphProperties);
            SetSinks(graphProperties);

            return new MediaGraphTopology("ContinuousRecording")
            {
                Properties = graphProperties
            };
        }

        // Add parameters to Topology
        private void SetParameters(MediaGraphTopologyProperties graphProperties)
        {
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("rtspUserName", MediaGraphParameterType.String)
            {
                Description = "rtsp source user name.",
                Default = "dummyUserName"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("rtspPassword", MediaGraphParameterType.SecretString)
            {
                Description = "rtsp source password.",
                Default = "dummyPassword"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("rtspUrl", MediaGraphParameterType.String)
            {
                Description = "rtsp Url"
            });
        }

        // Add sources to Topology
        private void SetSources(MediaGraphTopologyProperties graphProperties)
        {
            graphProperties.Sources.Add(new MediaGraphRtspSource("rtspSource", new MediaGraphUnsecuredEndpoint("${rtspUrl}")
            {
                Credentials = new MediaGraphUsernamePasswordCredentials("${rtspUserName}")
                {
                    Password = "${rtspPassword}"
                }
            })
            );
        }

        // Add sinks to Topology
        private void SetSinks(MediaGraphTopologyProperties graphProperties)
        {
            var graphNodeInput = new List<MediaGraphNodeInput>
            {
                { new MediaGraphNodeInput{NodeName = "rtspSource"} }
            };
            var cachePath = "/var/lib/azuremediaservices/tmp/";
            var cacheMaxSize = "2048";
            graphProperties.Sinks.Add(new MediaGraphAssetSink("assetSink", graphNodeInput, "sampleAsset-${System.GraphTopologyName}-${System.GraphInstanceName}", cachePath, cacheMaxSize)
            {
                SegmentLength = System.Xml.XmlConvert.ToString(TimeSpan.FromSeconds(30)),
            });
        }
    }
}