using System;
using System.Collections.Generic;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console.Topologies
{
// Motion Detection  
    public class MotionDetection : ITopology
    {
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