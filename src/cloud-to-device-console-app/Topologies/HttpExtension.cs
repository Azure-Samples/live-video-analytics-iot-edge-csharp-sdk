using System;
using System.Collections.Generic;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console.Topologies
{
//Connect external AI module using HTTP extension 
    public class HttpExtension : ITopology
    {
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
                    Name = "frameRate",
                    Type = MediaGraphParameterType.String,
                    Description = "Rate of the frames per second to be received from LVA.",
                    DefaultProperty = "2"
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
                { new MediaGraphFrameRateFilterProcessor {
                    Name = "frameRateFilter",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("rtspSource") }
                    },
                    MaximumFps = "${frameRate}"
                }},
                { new MediaGraphHttpExtension {
                    Name = "httpExtension",
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

        private List<MediaGraphSink> SetSinks()
        {
            return new List<MediaGraphSink> {
                { new MediaGraphIoTHubMessageSink {
                    Name = "hubSink",
                    HubOutputName = "inferenceOutput",
                    Inputs = new List<MediaGraphNodeInput> {
                        { new MediaGraphNodeInput("httpExtension") }
                    }
                }},
            };
        }
    }
}