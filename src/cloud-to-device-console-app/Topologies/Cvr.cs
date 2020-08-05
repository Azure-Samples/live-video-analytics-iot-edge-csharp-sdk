using System;
using System.Collections.Generic;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

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
            return new MediaGraphTopology(
                "ContinuousRecording",
                null,
                null,
                new MediaGraphTopologyProperties(
                    "Continuous video recording to an Azure Media Services Asset",
                    parameters: SetParameters(),
                    sources: SetSources(),
                    sinks: SetSinks()
                ));
        }

        // Add parameters to Topology
        private List<MediaGraphParameterDeclaration> SetParameters()
        {
            return new List<MediaGraphParameterDeclaration> {
                { new MediaGraphParameterDeclaration {
                    Name = "rtspUserName",
                    Type = MediaGraphParameterType.String,
                    Description = "rtsp source user name.",
                    DefaultProperty = "dummyUserName"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "rtspPassword",
                    Type = MediaGraphParameterType.SecretString,
                    Description = "rtsp source password.",
                    DefaultProperty = "dummyPassword"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "rtspUrl",
                    Type = MediaGraphParameterType.String,
                    Description = "rtsp Url"
                }},
            };
        }

        // Add sources to Topology
        private List<MediaGraphSource> SetSources()
        {
            return new List<MediaGraphSource> {
                { new MediaGraphRtspSource {
                    Name = "rtspSource",
                    Endpoint = new MediaGraphUnsecuredEndpoint {
                        Url = "${rtspUrl}",
                        Credentials = new MediaGraphUsernamePasswordCredentials {
                            Username = "${rtspUserName}",
                            Password = "${rtspPassword}"
                        }
                    }
                }},
            };
        }

        // Add sinks to Topology
        private List<MediaGraphSink> SetSinks()
        {
            return new List<MediaGraphSink> {
                { new MediaGraphAssetSink {
                    Name = "assetSink",
                    AssetNamePattern = "sampleAsset-${System.GraphTopologyName}-${System.GraphInstanceName}",
                    SegmentLength = TimeSpan.FromSeconds(30),
                    LocalMediaCacheMaximumSizeMiB = "2048",
                    LocalMediaCachePath = "/var/lib/azuremediaservices/tmp/",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("rtspSource") }
                    }
                }},
            };
        }
    }
}