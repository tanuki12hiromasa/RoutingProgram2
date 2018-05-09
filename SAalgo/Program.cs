﻿using System;

namespace Routing2
{
    class Program
    {
        const int width = 9;
        const int height = 9;
        const string mapfile = "neighbor_mat2.csv";
        const string destfile = "destination2.csv";
        static void Main(string[] args)
        {
            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            var pf = new PathFinder(width, height);
            pf.ex(mapfile, destfile);
        }
    }
}
