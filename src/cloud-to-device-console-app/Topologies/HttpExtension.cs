using Azure.Media.Analytics.Edge.Models;
using System;
using System.Collections.Generic;

namespace C2D_Console.Topologies
{
    /// <summary>
    /// Required modules:
    ///     1. Live Video Analytics
    ///     2. RTSP Simulator
    ///     3. Inferencing (i.e. yolov3) (if runs locally)
    /// </summary>
    public class HttpExtension : ITopology
    {
        /// <summary>
        /// Inferencing connected to external AI service through HTTP Topology ingredients
        ///    1. Parameters: rtspUserName, rtspPassword, rtspUrl, inferencingUrl, inferencingUserName, inferencingPassword, imageScaleMode, frameWidth, frameHeight, frameRate
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
            var graphProperties = new MediaGraphTopologyProperties
            {
                Description = "Analyzing live video using HTTP Extension to send images to an external inference engine",
            };

            SetParameters(graphProperties);
            SetProcessors(graphProperties);
            SetSources(graphProperties);
            SetSinks(graphProperties);

            return new MediaGraphTopology("InferencingWithHttpExtension")
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
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("inferencingUrl", MediaGraphParameterType.String)
            {
                Description = "inferencing Url",
                Default = "http://yolov3/score"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("inferencingUserName", MediaGraphParameterType.String)
            {
                Description = "inferencing endpoint user name.",
                Default = "dummyUserName"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("inferencingPassword", MediaGraphParameterType.String)
            {
                Description = "inferencing endpoint password.",
                Default = "dummyPassword"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("imageScaleMode", MediaGraphParameterType.String)
            {
                Description = "image scaling mode",
                Default = "preserveAspectRatio"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("frameWidth", MediaGraphParameterType.String)
            {
                Description = "Width of the video frame to be received from LVA.",
                Default = "416"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("frameHeight", MediaGraphParameterType.String)
            {
                Description = "Height of the video frame to be received from LVA.",
                Default = "416"
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

        // Add processors to Topology
        private void SetProcessors(MediaGraphTopologyProperties graphProperties)
        {

            var activationSignalOffset = "-PT5S";
            var minimumActivationTime = "PT30S";
            var maximumActivationTime = "PT30S";
            graphProperties.Processors.Add(
               new MediaGraphSignalGateProcessor(
                   "signalGateProcessor",
                    new List<MediaGraphNodeInput> {
                        new MediaGraphNodeInput() { NodeName = "iotMessageSource" },
                        new MediaGraphNodeInput() { NodeName = "rtspSource" }
                    },
                    activationSignalOffset,
                    minimumActivationTime,
                    maximumActivationTime
                )
               {
                   ActivationEvaluationWindow = "PT1S"
               }
           );

            graphProperties.Processors.Add(
               new MediaGraphHttpExtension(
                   "inferenceClient",
                    new List<MediaGraphNodeInput> {
                        new MediaGraphNodeInput() { NodeName = "rtspSource" }
                    },
                   new MediaGraphUnsecuredEndpoint("${inferencingUrl}")
                   {
                       Credentials = new MediaGraphUsernamePasswordCredentials("${inferencingUserName}")
                       {
                           Password = "${inferencingPassword}"
                       }
                   },
                   new MediaGraphImage
                   {
                       Scale = new MediaGraphImageScale(new MediaGraphImageScaleMode())
                       {
                           Mode = "${imageScaleMode}",
                           Width = "${frameWidth}",
                           Height = "${frameHeight}"
                       },
                       Format = new MediaGraphImageFormatBmp()
                   }
                )
           );
        }

        // Add sinks to Topology
        private void SetSinks(MediaGraphTopologyProperties graphProperties)
        {
            var hubGraphNodeInput = new List<MediaGraphNodeInput>
            {
                { new MediaGraphNodeInput{NodeName = "httpExtension"} }
            };

            graphProperties.Sinks.Add(new MediaGraphIoTHubMessageSink(
                "hubSink",
                hubGraphNodeInput,
                "inferenceOutput"
                )
            );
        }
    }
}