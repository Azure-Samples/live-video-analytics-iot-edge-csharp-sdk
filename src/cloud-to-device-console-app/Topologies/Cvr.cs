using System;
using System.Collections.Generic;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console.Topologies
{
//Continuous Video Recording 
    public class Cvr : ITopology
    {
        public MediaGraphTopology Build()
        {
            return new MediaGraphTopology(
                "CVRToAMSAsset",
                null,
                null,
                new MediaGraphTopologyProperties(
                    "Continuous video recording to an Azure Media Services Asset",
                    parameters: SetParameters(),
                    sources: SetSources(),
                    sinks: SetSinks()
                ));
        }

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