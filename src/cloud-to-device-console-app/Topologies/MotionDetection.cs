using Azure.Media.Analytics.Edge.Models;
using System;
using System.Collections.Generic;

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
            var graphProperties = new MediaGraphTopologyProperties
            {
                Description = "Analyzing live video to detect motion and emit events",
            };

            SetParameters(graphProperties);
            SetProcessors(graphProperties);
            SetSources(graphProperties);
            SetSinks(graphProperties);

            return new MediaGraphTopology("MotionDetection")
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

        // Add processors to Topology
        private void SetProcessors(MediaGraphTopologyProperties graphProperties)
        {

            graphProperties.Processors.Add(
                new MediaGraphMotionDetectionProcessor(
                    "motionDetection",
                    new List<MediaGraphNodeInput> {
                        new MediaGraphNodeInput() { NodeName = "rtspSource" }
                    }
                )
                {
                    Sensitivity = "medium"
                }
            );
        }

        // Add sinks to Topology
        private void SetSinks(MediaGraphTopologyProperties graphProperties)
        {
            var hubGraphNodeInput = new List<MediaGraphNodeInput>
            {
                { new MediaGraphNodeInput{NodeName = "inferenceClient"} }
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