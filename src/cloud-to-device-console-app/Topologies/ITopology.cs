using Microsoft.Azure.Media.LiveVideoAnalytics.Edge.Models;

namespace C2D_Console.Topologies
{
//Build MediaGraphTopology 
    public interface ITopology
    {
        public MediaGraphTopology Build();
    }
}