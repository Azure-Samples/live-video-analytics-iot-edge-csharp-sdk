using Azure.Media.Analytics.Edge.Models;
using C2D_Console.Topologies;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace C2D_Console
{
    class Program
    {                
        static ServiceClient edgeClient;
        static string deviceId;
        static string moduleId;

        static MediaGraphTopology BuildTopology()
        {
            // NOTE: bare way to offer picking one of the available Topologies.
            //       A more powerfull way could discover what's avaliable to offer (i.e. using ITopology).
            string[] options = {
                "Continuous Video Recording",
                "Motion Detection",
                "Event Based Video Recording to edge device",
                "Inference using HTTP Extension",
                "Inference using ObjectCounter module"};

            // NOTE: present `options` and ask user to pick one.
            //       Built as is for sample purposes, leaving for the reader
            //       to improve on it.
            Console.WriteLine("\nThese are the available topologies...");
            for (int index = 0; index < options.Length; index++)
            {
                Console.WriteLine($"\t{index + 1}: {options[index]}");
            }

            Console.Write($"Pick your topology (1 to {options.Length}) and press <ENTER>: ");
            int option;
            string input = Console.ReadLine();

            while(!Int32.TryParse(input, out option))
            {
                Console.WriteLine("Not a valid option, try again...");

                input = Console.ReadLine();
            }

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
            MediaGraphTopology topology,
            string url,
            string userName,
            string password)
        {
            var graphInstanceName = $"Sample-Graph-1";
            var properties = new MediaGraphInstanceProperties
            {
                TopologyName = topology.Name,
                Description = "Sample graph description"
            };

            properties.Parameters.Add(new MediaGraphParameterDefinition("rtspUrl", url));
            properties.Parameters.Add(new MediaGraphParameterDefinition("rtspUserName", userName));
            properties.Parameters.Add(new MediaGraphParameterDefinition("rtspPassword", password));


            var result = new MediaGraphInstance(graphInstanceName) {
                Properties = properties    
            };


            // NOTE: screen print for current Graph Instance status
            if (PresentParamsProgress(result, topology))
            {
                // NOTE: ask for any parameter the user wants to override
                topology.Properties.Parameters.ToList().ForEach(p => {
                    if (! new string[] {"rtspUrl", "rtspUserName", "rtspPassword"}.Contains(p.Name))
                    {
                        var input = GetInputFor(p.Name, p.Type);
                        if (!string.IsNullOrWhiteSpace(input))
                            properties.Parameters.Add(new MediaGraphParameterDefinition(p.Name, input));
                    }
                });

                // NOTE: screen print for current (and ready to run) Graph Instance
                PresentParamsProgress(result, topology, true);
            }
            return result;
        }

        static bool PresentParamsProgress(MediaGraphInstance graphInstance, MediaGraphTopology topology, bool post = false)
        {
            // NOTE: before printing Graph Instance to screen, we make sure no `SecretString` value reaches the output.
            var cleanedParameters = new List<MediaGraphParameterDefinition>();

            // Mask sensitive data
            if (graphInstance.Properties.Parameters?.Count > 0)
            {
                graphInstance.Properties.Parameters.ToList().ForEach(gp => {
                    var tp = topology.Properties.Parameters.FirstOrDefault(tp => tp.Name == gp.Name);
                    if (tp != null) {
                        if (tp.Type == MediaGraphParameterType.SecretString)
                        {
                            cleanedParameters.Add(new MediaGraphParameterDefinition(gp.Name, gp.Value));
                            gp.Value = "**********";
                        }
                    }
                });
            }

            Console.WriteLine(
                JsonSerializer.Serialize(graphInstance, new JsonSerializerOptions { WriteIndented = true})
            );

            var orig = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nParameter summary for the Graph Instance");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.ForegroundColor = orig;

            // Remove mask
            cleanedParameters.ForEach(cleanParameter => {
                var parameter = graphInstance.Properties.Parameters.FirstOrDefault(originalParam => originalParam.Name == cleanParameter.Name);
                if (parameter.Value != null)
                {
                    parameter.Value = cleanParameter.Value;
                }
            });

            // NOTE: initial run (post = false), informs supplied and not supplied parameters.
            //       Second run (post = true), informs each parameter status after overriding opt
            var containsNotSuppliedParams = false;
            foreach(var param in topology.Properties.Parameters)
            {
                if (!graphInstance.Properties.Parameters.Any(p => p.Name == param.Name))
                {
                    Console.ForegroundColor = (!post)?ConsoleColor.Red:ConsoleColor.Yellow;
                    Console.WriteLine((!post)?$"\t\"{param.Name}\" not supplied.":$"\t\"{param.Name}\" not supplied. Using default value.");
                    containsNotSuppliedParams = true;
                    Console.ForegroundColor = orig;
                } else {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\t\"{param.Name}\" supplied.");
                    Console.ForegroundColor = orig;
                }
            }

            if(!post && containsNotSuppliedParams)
                Console.Write("\nYou'll be offered to supply values for each remaining (red) parameter.");
            
            Console.Write("\nPress <ENTER> to continue... ");
            Console.ReadLine();

            return containsNotSuppliedParams;
        }

        static string GetInputFor(string parameterName, MediaGraphParameterType parameterType)
        {
            // NOTE: gets the user's input to override a parameters value. In case the parameter
            //       is a SecretString, it makes sure no keystroke gets echoed to output.
            Console.WriteLine($"Input the desired value for parameter \"{parameterName}\" and press <ENTER> (leave blank to use default)");
            if (parameterType != MediaGraphParameterType.SecretString)
            {
                var result = Console.ReadLine();
                return result;
            } else {
                var pass = string.Empty;
                ConsoleKey key;
                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        Console.Write("\b \b");
                        pass = pass[0..^1];
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Console.Write("*");
                        pass += keyInfo.KeyChar;
                    }
                } while (key != ConsoleKey.Enter);
                Console.Write("\n");
                return pass;
            }
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

                deviceId = appSettings["deviceId"];
                moduleId = appSettings["moduleId"];
                edgeClient = ServiceClient.CreateFromConnectionString(appSettings["IoThubConnectionString"]);
    
                // NOTE: build GraphTopology based on the selected Topology (check how it's done),
                //       and a GraphInstance.
                var topology = BuildTopology();
                // NOTE: different topologies, need a minimum set of modules running. There're 3 deployment manifests
                //       under /src/edge folder. Each topology has it's minimum module requirements to run commented
                //       onto them.
                var instance = BuildInstance(topology, rtspUrl, rtspUserName, rtspPassword);

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

        /// <summary>
        /// Create an instance and invoke the CloudToDeviceMethod to execute a Direct Method on the device.
        /// </summary>
        /// <param name="request">The MethodRequest to execute</param>
        static async Task CreateStepOperation(MethodRequest request)
        {
            var directMethod = new CloudToDeviceMethod(request.MethodName);
            directMethod.SetPayloadJson(request.GetPayloadAsJson());

            var result = await edgeClient.InvokeDeviceMethodAsync(deviceId, moduleId, directMethod);

            if (result.Status < 400)
            {
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                await PrintMessage(result.GetPayloadAsJson(), ConsoleColor.Red);
            }
        }

        static List<Step> Orchestrate(MediaGraphTopology topology, MediaGraphInstance instance)
        {
            // NOTE: prepares the ordered script to be followed. Each one modeled after a `Step` class.
            return new List<Step>() {
                    new Step {
                        Enabled = false,
                        Name = "GraphTopologyList",
                        Op = () => CreateStepOperation(new MediaGraphTopologyListRequest())
                    },
                    new Step {
                        Enabled = false,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = false,
                        Name = "GraphInstanceList",
                        Op = () => CreateStepOperation(new MediaGraphInstanceListRequest())
                    },
                    new Step {
                        Enabled = false,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) },
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologySet",
                        Op = () => CreateStepOperation(new MediaGraphTopologySetRequest(topology))
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceSet",
                        Op = () => CreateStepOperation(new MediaGraphInstanceSetRequest(instance))
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceActivate",
                        Op = () => CreateStepOperation(new MediaGraphInstanceActivateRequest(instance.Name)) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceList",
                        Op = () => CreateStepOperation(new MediaGraphInstanceListRequest())
                    },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("The topology will now be deactivated.\nPress <ENTER> to continue", ConsoleColor.Yellow, true) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceDeactivate",
                        Op = () => CreateStepOperation(new MediaGraphInstanceDeActivateRequest(instance.Name)) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceDelete",
                        Op = () => CreateStepOperation(new MediaGraphInstanceDeleteRequest(instance.Name)) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphInstanceList",
                        Op = () => CreateStepOperation(new MediaGraphInstanceListRequest())
                    },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologyDelete",
                        Op = () => CreateStepOperation(new MediaGraphInstanceDeleteRequest(topology.Name)) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) 
                    },
                    new Step {
                        Enabled = true,
                        Name = "GraphTopologyList",
                        Op = () => CreateStepOperation(new MediaGraphTopologyListRequest())
                    },
                    new Step {
                        Enabled = true,
                        Name = "WaitForInput",
                        Op = () => PrintMessage("Press <ENTER> to continue", ConsoleColor.Yellow, true) 
                    },
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
