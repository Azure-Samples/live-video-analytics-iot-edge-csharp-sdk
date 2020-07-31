using System;
using System.Collections.Generic;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console.Topologies
{
//Connect to inferencing using HTTP extension and then, to an external module using IoT Hub routing capabilities
    
    /// <summary>
    /// Required modules:
    ///     1. Live Video Analytics
    ///     2. RTSP Simulator
    ///     3. Inferencing (i.e. yolov3) (if runs locally)
    ///     4. Object Counter custom module (/src/edge)
    /// </summary>
    public class EvrHubAssets : ITopology
    {
        /// <summary>
        /// External Inferencing and Analysis Topology ingredients
        ///    1. Parameters: rtspUserName, rtspPassword, rtspUrl, hubSourceInput, inferencingUrl, inferencingUserName, inferencingPassword, imageEncoding, imageScaleMode, frameWidth, frameHeight, hubSinkOutputName
        ///    2. Sources: `MediaGraphRtspSource`
        ///    3. Processors: `MediaGraphFrameRateFilterProcessor`, `MediaGraphHttpExtension`
        ///    4. Sinks: `MediaGraphIoTHubMessageSink`
        ///
        /// </summary>
        ///
        /// <remark>
        /// For additional info on Media Graph and its pieces, please refer to https://docs.microsoft.com/en-us/azure/media-services/live-video-analytics-edge/media-graph-concept
        /// </remark>
        public MediaGraphTopology Build()
        {
            return new MediaGraphTopology(
                "InferencingWithHttpExtension",
                null,
                null,
                new MediaGraphTopologyProperties(
                    "Analyzing live video using HTTP Extension to send images to an external inference engine",
                    parameters: SetParameters(),
                    sources: SetSources(),
                    processors: SetProcessors(),
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
                { new MediaGraphParameterDeclaration {
                    Name = "hubSourceInput",
                    Type = MediaGraphParameterType.String,
                    Description = "input name for hub source",
                    DefaultProperty = "recordingTrigger"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "inferencingUrl",
                    Type = MediaGraphParameterType.String,
                    Description = "inferencing Url",
                    DefaultProperty = "http://yolov3/score"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "inferencingUserName",
                    Type = MediaGraphParameterType.String,
                    Description = "inferencing endpoint user name.",
                    DefaultProperty = "dummyUserName"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "inferencingPassword",
                    Type = MediaGraphParameterType.SecretString,
                    Description = "inferencing endpoint password.",
                    DefaultProperty = "dummyPassword"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "imageEncoding",
                    Type = MediaGraphParameterType.String,
                    Description = "image encoding for frames",
                    DefaultProperty = "bmp"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "imageScaleMode",
                    Type = MediaGraphParameterType.String,
                    Description = "image scaling mode",
                    DefaultProperty = "preserveAspectRatio"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "frameWidth",
                    Type = MediaGraphParameterType.String,
                    Description = "Width of the video frame to be received from LVA.",
                    DefaultProperty = "416"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "frameHeight",
                    Type = MediaGraphParameterType.String,
                    Description = "Height of the video frame to be received from LVA.",
                    DefaultProperty = "416"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "hubSinkOutputName",
                    Type = MediaGraphParameterType.String,
                    Description = "hub sink output name",
                    DefaultProperty = "detectedObjects"
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
                { new MediaGraphIoTHubMessageSource {
                    Name = "iotMessageSource",
                    HubInputName = "${hubSourceInput}"
                }},
            };
        }

        // Add processors to Topology
        private List<MediaGraphProcessor> SetProcessors()
        {
            return new List<MediaGraphProcessor> {
                { new MediaGraphSignalGateProcessor {
                    Name = "signalGateProcessor",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("iotMessageSource") },
                        { new MediaGraphNodeInput("rtspSource") }
                    },
                    ActivationEvaluationWindow = "PT1S",
                    ActivationSignalOffset = "-PT5S",
                    MinimumActivationTime = "PT30S",
                    MaximumActivationTime = "PT30S"
                }},
                { new MediaGraphFrameRateFilterProcessor {
                    Name = "frameRateFilter",
                    MaximumFps = "1.0",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("rtspSource") }
                    },
                }},
                { new MediaGraphHttpExtension {
                    Name = "inferenceClient",
                    Endpoint = new MediaGraphUnsecuredEndpoint {
                        Url = "${inferencingUrl}",
                        Credentials = new MediaGraphUsernamePasswordCredentials {
                            Username = "${inferencingUserName}",
                            Password = "${inferencingPassword}"
                        }
                    },
                    Image = new MediaGraphImage {
                        Scale = new MediaGraphImageScale {
                            Mode = "${imageScaleMode}",
                            Width = "${frameWidth}",
                            Height = "${frameHeight}"
                        },
                        Format = new MediaGraphImageFormatEncoded {
                            Encoding = "${imageEncoding}"
                        }
                    },
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("frameRateFilter") }
                    }
                }},
            };
        }

        // Add sinks to Topology
        private List<MediaGraphSink> SetSinks()
        {
            return new List<MediaGraphSink> {
                { new MediaGraphIoTHubMessageSink {
                    Name = "hubSink",
                    HubOutputName = "${hubSinkOutputName}",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("inferenceClient") }
                    }
                }},
                { new MediaGraphAssetSink {
                    Name = "assetSink",
                    AssetNamePattern = "sampleAssetFromEVR-LVAEdge-${System.DateTime}",
                    SegmentLength = TimeSpan.FromSeconds(30),
                    LocalMediaCacheMaximumSizeMiB = "2048",
                    LocalMediaCachePath = "/var/lib/azuremediaservices/tmp/",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("signalGateProcessor") }
                    }
                }},
            };
        }
    }
}