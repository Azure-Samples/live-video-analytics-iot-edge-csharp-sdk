using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console.Topologies
{
//Build MediaGraphTopology 
    public interface ITopology
    {
        // NOTE: Each Topology implementing this Interface, must expose a
        //       `MediaGraphTopology` builder.
        MediaGraphTopology Build();
    }
}