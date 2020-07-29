using C2D_Console.Topologies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge;
using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console
{
    class Program
    {                
        static ILiveVideoAnalyticsEdgeClient edgeClient;
        static string deviceId;
        static string moduleId;

        static MediaGraphTopology BuildTopology()
        {
            // NOTE: bare way to offer picking one of the available Topologies.
            //       A more powerfull way could discover what's avaliable to offer (i.e. using ITopology).
            string[] options = { "Cvr", "Motion", "Evr", "Extension", "Inference with ObjectCounter module"};

            // NOTE: present `options` and ask user to pick one.
            //       Built as is for sample purposes, leaving for the reader
            //       to improve on it.
            Console.WriteLine("\nThese are the available topologies...");
            for (int index = 0; index < options.Length; index++)
            {
                Console.WriteLine($"\t{index + 1}: {options[index]}");
            }

            Console.Write($"Pick your topology (1 to {options.Length}) and press <ENTER>: ");
            // TODO: input validation
            var option = Int32.Parse(Console.ReadLine());

            ITopology result = null;
            switch (option)
            {
                case 1:
                    result = new Cvr();
                    break;
                case 2:
                    result = new MotionDetection();
                    break;
                case 3:
                    result = new EvrFiles();
                    break;
                case 4:
                    result = new HttpExtension();
                    break;
                case 5:
                    result = new EvrHubAssets();
                    break;
            }

            // NOTE: build and return the topology.
            return result.Build();
        }

        static MediaGraphInstance BuildInstance(
            string topologyName,
            string url,
            string userName,
            string password)
        {
            // NOTE: use RTSP simulator values if not provided. Built as is
            //       for demo purposes only.
            if (string.IsNullOrWhiteSpace(url)
                || string.IsNullOrWhiteSpace(userName)
                || string.IsNullOrWhiteSpace(password) )
            {
                url = "rtsp://rtspsim:554/media/camera-300s.mkv";
                userName = "testuser";
                password = "testpassword";    
            }
            
            return new MediaGraphInstance {
                Name = $"Sample-Graph-1",
                Properties = new MediaGraphInstanceProperties {
                    TopologyName = topologyName,
                    Description = "Sample graph description",
                    Parameters = new List<MediaGraphParameterDefinition> {
                        { new MediaGraphParameterDefinition {
                            Name = "rtspUrl",
                            Value = url
                        }},
                        { new MediaGraphParameterDefinition {
                            Name = "rtspUserName",
                            Value = userName
                        }},
                        { new MediaGraphParameterDefinition {
                            Name = "rtspPassword",
                            Value = password
                        }},
                    }
                }
            };
        }

        static async Task Main(string[] args)
        {
            try
            {
                // NOTE: Read app configuration
                IConfigurationRoot appSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();             

                // NOTE: Configure the IoT Hub Service client
                deviceId = appSettings["deviceId"];
                moduleId = appSettings["moduleId"];

                // NOTE: Get the camera's RTSP parameters.
                //       Place these into `appsettings.json` for a proper read.
                var rtspUrl = appSettings["rtspUrl"];
                var rtspUserName = appSettings["rtspUserName"];
                var rtspPassword = appSettings["rtspPassword"];

                // NOTE: create the ILiveVideoAnalyticsEdgeClient (this is the SDKs client
                //       that bridges your application to LVAEdge module)
                edgeClient = MediaServicesEdgeClientFactory.Create(
                    ServiceClient.CreateFromConnectionString(appSettings["IoThubConnectionString"]),
                    deviceId,
                    moduleId
                );
    
                // NOTE: build GraphTopology based on the selected Topology (check how it's done),
                //       and a GraphInstance.
                var topology = BuildTopology();
                // NOTE: different topologies, need a minimum set of modules running. There're 3 deployment manifests
                //       under /src/edge folder. Each topology has it's minimum module requirements to run commented
                //       onto them.
                var instance = BuildInstance(topology.Name, rtspUrl, rtspUserName, rtspPassword);

                // NOTE: obtain the ordered operation set.
                var steps = Orchestrate(topology, instance);

                try {
                    // NOTE: run each enabled operation.
                    foreach (var step in steps)
                    {
                        if (step.Enabled) {
                            await PrintMessage("\n--------------------------------------------------------------------------\n", ConsoleColor.Cyan);
                            Console.WriteLine("Executing operation " + step.Name);
                            await step.Op.Invoke();
                        }
                    }
                } catch (AggregateException ex)
                {
                    await PrintMessage(ex.Flatten().Message, ConsoleColor.Red);
                }
            }
            catch(Exception ex)
            {
                await PrintMessage(ex.ToString(), ConsoleColor.Red);
            }
        }

        static List<Step> Orchestrate(MediaGraphTopology topology, MediaGraphInstance instance)
        {
            // NOTE: prepares the ordered script to be followed. Each one modeled after a `Step` class.
            return new List<Step>() {
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologyList",
                        Op = () => edgeClient.GraphTopologyListAsync()
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceList",
                        Op = () => edgeClient.GraphInstanceListAsync()
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologySet",
                        Op = () => edgeClient.GraphTopologySetAsync(topology)
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceSet",
                        Op = () => edgeClient.GraphInstanceSetAsync(instance)
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceActivate",
                        Op = () => edgeClient.GraphInstanceActivateAsync(instance.Name) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceList",
                        Op = () => edgeClient.GraphInstanceListAsync()
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("The topology will now be deactivated.\nPress <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceDeactivate",
                        Op = () => edgeClient.GraphInstanceDeactivateAsync(instance.Name) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceDelete",
                        Op = () => edgeClient.GraphInstanceDeleteAsync(instance.Name) },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceList",
                        Op = () => edgeClient.GraphInstanceListAsync()
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologyDelete",
                        Op = () => edgeClient.GraphTopologyDeleteAsync(topology.Name) },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologyList",
                        Op = () => edgeClient.GraphTopologyListAsync()
                            .ContinueWith(t => Console.WriteLine(
                                JsonSerializer.Serialize(t.Result, new JsonSerializerOptions { WriteIndented = true}))) },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                };
        }

        static Task PrintMessage(string message, ConsoleColor color, bool waits = false)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();

            if (waits) Console.ReadLine();

            return Task.FromResult(0);
        }
    }
}
