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
    public class EvrFiles : ITopology
    {
        /// <summary>
        /// Event based video recording saving onto Files Topology ingredients
        ///    1. Parameters: rtspUserName, rtspPassword, rtspUrl, motionSensitivity, fileSinkOutputName
        ///    2. Sources: `MediaGraphRtspSource`
        ///    3. Processors: `MediaGraphMotionDetectionProcessor`, `MediaGraphSignalGateProcessor`
        ///    4. Sinks: `MediaGraphFileSink`
        ///
        /// </summary>
        ///
        /// <remark>
        /// For additional info on Media Graph and its pieces, please refer to https://docs.microsoft.com/en-us/azure/media-services/live-video-analytics-edge/media-graph-concept
        /// </remark>
        public MediaGraphTopology Build()
        {
            return new MediaGraphTopology(
                "EVRToFilesOnMotionDetection",
                null,
                null,
                new MediaGraphTopologyProperties(
                    "Event-based video recording to local files based on motion events",
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
                    Name = "motionSensitivity",
                    Type = MediaGraphParameterType.String,
                    Description = "motion detection sensitivity",
                    DefaultProperty = "medium"
                }},
                { new MediaGraphParameterDeclaration {
                    Name = "fileSinkOutputName",
                    Type = MediaGraphParameterType.String,
                    Description = "file sink output name",
                    DefaultProperty = "filesinkOutput"
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
                    Sensitivity = "${motionSensitivity}",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("rtspSource") }
                    }
                }},
                { new MediaGraphSignalGateProcessor {
                    Name = "signalGateProcessor",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("motionDetection") },
                        { new MediaGraphNodeInput("rtspSource") }
                    },
                    ActivationEvaluationWindow = "PT1S",
                    ActivationSignalOffset = "PT0S",
                    MinimumActivationTime = "PT5S",
                    MaximumActivationTime = "PT5S"
                }},
            };
        }

        // Add sinks to Topology
        private List<MediaGraphSink> SetSinks()
        {
            return new List<MediaGraphSink> {
                { new MediaGraphFileSink {
                    Name = "fileSink",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("signalGateProcessor") }
                    },
                    FilePathPattern = "/var/media/sampleFilesFromEVR-${fileSinkOutputName}-${System.DateTime}"
                }},
            };
        }
    }
}