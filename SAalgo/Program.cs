using System;

namespace Routing2
{
    class Program
    {
        const int width = 9;
        const int height = 9;
        const string mapfile = "neighbor_mat3.csv";
        const string destfile = "destination4.csv";
        static void Main(string[] args)
        {
            bool finalReturn = false;
            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            var pf = new PathFinder(width, height, finalReturn);
            pf.ex(mapfile, destfile);
        }
    }
}
