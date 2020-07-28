using System;
using System.Threading.Tasks;

namespace C2D_Console
{
    public class Step
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public Func<Task> Op { get; set; }
    }
}
