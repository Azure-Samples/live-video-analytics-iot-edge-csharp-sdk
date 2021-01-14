using Azure.Media.Analytics.Edge.Models;
using System.Collections.Generic;

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
            var graphProperties = new MediaGraphTopologyProperties
            {
                Description = "Event - based video recording to local files based on motion events",
            };

            SetParameters(graphProperties);
            SetSources(graphProperties);
            SetProcessors(graphProperties);
            SetSinks(graphProperties);

            return new MediaGraphTopology("EventsToFilesMotionDetection")
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
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("motionSensitivity", MediaGraphParameterType.String)
            {
                Description = "motion detection sensitivity",
                Default = "medium"
            });
            graphProperties.Parameters.Add(new MediaGraphParameterDeclaration("fileSinkOutputName", MediaGraphParameterType.String)
            {
                Description = "file sink output name",
                Default = "filesinkOutput"
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
                    Sensitivity = "${motionSensitivity}"
                }
            );

            var activationSignalOffset = "PT0S";
            var minimumActivationTime = "PT5S";
            var maximumActivationTime = "PT5S";
            graphProperties.Processors.Add(
               new MediaGraphSignalGateProcessor(
                   "signalGateProcessor", 
                    new List<MediaGraphNodeInput> { 
                        new MediaGraphNodeInput() { NodeName = "motionDetection" }, 
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
        }

        // Add sinks to Topology
        private void SetSinks(MediaGraphTopologyProperties graphProperties)
        {
            var graphNodeInput = new List<MediaGraphNodeInput>
            {
                { new MediaGraphNodeInput{NodeName = "signalGateProcessor"} }
            };
            var baseDirectoryPath = "/var/media";
            var maximumSizeMiB = "512";
            var filePathPattern = "sampleFilesFromEVR-${fileSinkOutputName}-${System.DateTime}";
            graphProperties.Sinks.Add(new MediaGraphFileSink("fileSink", graphNodeInput, baseDirectoryPath, filePathPattern, maximumSizeMiB));
        }
    }
}