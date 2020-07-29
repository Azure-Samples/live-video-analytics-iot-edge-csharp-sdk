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
    public class MotionDetection : ITopology
    {
        /// <summary>
        /// Motion Detection based event emitter Topology ingredients
        ///    1. Parameters: rtspUserName, rtspPassword, rtspUrl
        ///    2. Sources: `MediaGraphRtspSource`
        ///    3. Processors: `MediaGraphMotionDetectionProcessor`
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
                "MotionDetection",
                null,
                null,
                new MediaGraphTopologyProperties(
                    "Analyzing live video to detect motion and emit events",
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

        // Add processors to Topology
        private List<MediaGraphProcessor> SetProcessors()
        {
            return new List<MediaGraphProcessor> {
                { new MediaGraphMotionDetectionProcessor {
                    Name = "motionDetection",
                    Sensitivity = "medium",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("rtspSource") }
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
                    HubOutputName = "inferenceOutput",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("motionDetection") }
                    }
                }},
            };
        }
    }
}