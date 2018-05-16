﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Routing2
{
    public class Destination
    {
        public string name;
        public int pos;
        public int[] cost;
        public List<int>[] route;
        public int weight = 80;
        public bool AM = true;
        public bool PM = true;
    }

    public class Node { public int pos, prev, cost; };

    public class PathFinder
    {
        int _width;int _height;
        int nCross; //交差点の数 width*height 
        int[,] map; //グラフ（地図）データ
        Destination[] dest; //配達地点　並びはcsvの順
        List<int> path; //配達順
        int startpoint; //開始点/終着点
        int[] isDest; //その場所がDestかどうか(Destならその番号、違うなら-1)
        protected string outfile = "betterpath.txt";

        public PathFinder(int width,int height)
        {
            _width = width; _height = height;
            nCross = _width * _height;
            map = new int[nCross, nCross];
            isDest = new int[nCross];
            for (int i =0;i<isDest.Length;i++) isDest[i] = -1;
        }

        public int ex(string mapfile,string destsfile) //一連の計算を行う
        {
            ReadMap(mapfile);
            ReadDest(destsfile);
            findRoute();
            makePath(dest, startpoint, 1000 , out path);
            WritePath();
            return 0;
        }

        void findRoute()
        {

            for(int i = 0; i < dest.Length; i++)
            {                
                int dLeft = dest.Length;
                var nextNodeList = new List<Node>();
                var visitedNodes = new Node[nCross];
                nextNodeList.Add(new Node { pos = dest[i].pos, prev = -1, cost = 0 });

                while (dLeft > 0)
                {
                    nextNodeList.Sort((a, b) => a.cost - b.cost);
                    var node = nextNodeList[0];
                    nextNodeList.RemoveAt(0);
                    if (visitedNodes[node.pos] == null)
                    {
                        visitedNodes[node.pos] = node;
                        if (isDest[node.pos] != -1) dLeft--;
                        for (int n = 0; n < nCross; n++)
                        {
                            if (map[node.pos, n] != 0)
                            {
                                nextNodeList.Add(new Node { pos = n, prev = node.pos, cost = node.cost + map[node.pos,n] });
                            }
                        }
                    }
                }
                for(int j = 0; j < dest.Length; j++) //prevを辿ってrouteに格納
                {
                    if (i == j) continue;
                    var pos = dest[j].pos;
                    while (pos != dest[i].pos)
                    {
                        dest[i].route[j].Add(pos);
                        pos = visitedNodes[pos].prev;
                    }
                    dest[i].route[j].Reverse();
                    dest[i].cost[j] = visitedNodes[dest[j].pos].cost;
                }
                Console.WriteLine(dest[i].name + ":done");
            }
            Console.WriteLine("findRoute:done");
            WriteRoute();
        }

        virtual protected void makePath(Destination[] dest,int startpoint,int wCap,out List<int> path)
        {

            var path1 = new List<int>();
            var path2 = new List<int>();
            //まずそれなりの解を作成する
            var yetList = new List<int>();
            for (int i = 0; i < dest.Length; i++) if (i != startpoint) yetList.Add(i);
            path1.Add(startpoint);
            path2.Add(startpoint);
            for(int t = 1; t <= 2; t++)
            {
                int cur = startpoint;
                var tpath = (t == 1) ? path1 : path2;
                while (tpath.Count <= dest.Length / 2 && yetList.Count > 0)
                {
                    int mincost = int.MaxValue;
                    int mincostsp = int.MaxValue;
                    int mindest = startpoint;
                    for(int i = 0; i < yetList.Count; i++)
                    {
                        var cost = dest[cur].cost[yetList[i]];
                        var costsp = dest[startpoint].cost[yetList[i]];
                        if (cost < mincost || cost == mincost && costsp < mincostsp)
                        {   mincost = cost; mincostsp = costsp; mindest = yetList[i];  }
                    }
                    yetList.Remove(mindest);
                    tpath.Add(mindest);
                    cur = mindest;
                }
                tpath.Add(startpoint);
            }

            Console.WriteLine("SA start.");

            var bestpath = new List<int>(path1); bestpath.AddRange(path2);
            int bestsum = SumCost(bestpath, 9 * 6);
            double T = 1000;
            const double alpha = 0.99999;
            const double beta = 1.000005;

            var starttime = DateTime.Now;
            var timelimit = new TimeSpan(0, 0, 20);
            var numofnode = dest.Length;
            var random = new Random(3882);
            Int64 count = 0;
            while (DateTime.Now - starttime < timelimit)
            {
                var nextpath1 = new List<int>(path1);
                var nextpath2 = new List<int>(path2);

                for(bool okeyflg = false; !okeyflg;) //合法でランダムな次状態を作成
                {
                    int transdest;
                    var fromNum = random.Next(1, numofnode);
                    var toNum = random.Next(1, numofnode+ 1 );

                    if(fromNum < nextpath1.Count - 1) //移動元から削除
                    {  transdest = nextpath1[fromNum]; nextpath1.RemoveAt(fromNum); }
                    else
                    { transdest = nextpath2[fromNum - (nextpath1.Count - 2)]; nextpath2.RemoveAt(fromNum - (nextpath1.Count - 2)); }

                    if(toNum < nextpath1.Count) //移動先に挿入
                    { nextpath1.Insert(toNum, transdest); }
                    else
                    { nextpath2.Insert(toNum - (nextpath1.Count - 1), transdest); }
                    
                    if(SumWeight(nextpath1) <= wCap && SumWeight(nextpath2) <= wCap)
                         okeyflg = true;
                    else
                    { nextpath1 = new List<int>(path1); nextpath2 = new List<int>(path2); }
                }

                var E1 = SumCost(nextpath1) + SumCost(nextpath2);
                var deltaE = E1 - (SumCost(path1) + SumCost(path2));
                if (deltaE < 0 || Math.Exp(-deltaE / T) > random.NextDouble())
                {
                    if (E1 < bestsum) {
                        bestsum = E1; bestpath = new List<int>(nextpath1);
                        bestpath.RemoveAt(bestpath.Count - 1);
                        bestpath.AddRange(nextpath2); }
                    path1 = nextpath1; path2 = nextpath2;
                    T *= alpha;
                }
                else T *= beta;

                count++;
            }

            Console.WriteLine("SA finish. Count:" + count);

            path = bestpath;
        }

        int SumWeight(List<int> path)
        {
            int sum = 0;
            for (int i = 0; i < path.Count; i++) sum += dest[path[i]].weight;
            return sum;
        }
        int SumCost(List<int> path,int starttime) //timeはAM0:00を始点とし、10minで1単位であると定義する。
        {
            int sum = 0;
            var time = starttime;
            for (int i = 0; i < path.Count - 1; i++)
            {
                time = (time + dest[path[i]].cost[path[i + 1]]) % (6 * 24);
                sum += dest[path[i]].cost[path[i + 1]];
                if (time < 12 * 6)
                {
                    if (!dest[path[i]].AM)
                    {
                        sum += 12 * 6 - time;
                        time = 12 * 6;
                    }
                }
                else
                {
                    if (!dest[path[i]].AM)
                    {
                        sum += 24 * 6 - time;
                        time = 0;
                    }
                }
                
            } 
            return sum;
        }

        void ReadMap(string file)
        {
            try
            {
                using (var str = new System.IO.StreamReader(file))
                {
                    for (int i = 0; i < nCross; i++)
                    {
                        var line = str.ReadLine();
                        var nodes = line.Split(',');
                        for (int j = 0; j < nCross; j++) map[i, j] = int.Parse(nodes[j]);
                    }
                }
                System.Console.WriteLine("reading map done");
                for(int i = 0; i < nCross; i++)
                {
                    for (int j = 0; j < nCross; j++) System.Console.Write(map[i, j] + " ");
                    System.Console.Write('\n');
                }
            }
            catch(System.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }

        }

        void ReadDest(string file)
        {
            try
            {
                using(var str = new System.IO.StreamReader(file))
                {
                    var dList = new List<Destination>();
                    while (!str.EndOfStream)
                    {
                        var line = str.ReadLine();
                        var nodes = line.Split(',');
                        dList.Add(new Destination { name = nodes[0], pos = int.Parse(nodes[1]) - 1, weight= int.Parse(nodes[2]) });
                    }
                    dest = dList.ToArray();
                    for (int i = 0; i < dest.Length; i++)
                    {
                        dest[i].cost = new int[dest.Length];
                        dest[i].route = new List<int>[dest.Length];
                        for (int j = 0; j < dest.Length; j++) dest[i].route[j] = new List<int>();
                        if (dest[i].name == "SP") startpoint = i;
                        isDest[dest[i].pos] = i;
                    }
                    for (int i = 0; i < dest.Length; i++) Console.WriteLine("name:" + dest[i].name + " pos:" + dest[i].pos);
                    Console.WriteLine("startpoint:" + startpoint + "(" + dest[startpoint].pos + ")");
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }

        }

        public void WriteRoute()
        {
            for(int i = 0; i < dest.Length; i++)
            {
                Console.Write(dest[i].name + ":");
                for(int j = 0; j < dest.Length; j++)
                {
                    Console.Write(dest[i].cost[j]+" ");
                }
                Console.WriteLine();
            }
        }

        public void WritePath()
        {
            try
            {
                using(var stw = new System.IO.StreamWriter(outfile))
                {
                    for(int i = 0; i < path.Count-1; i++)
                    {
                        stw.Write(dest[path[i]].name+": ");
                        for (int j = 0; j < dest[path[i]].route[path[i+1]].Count; j++) stw.Write(dest[path[i]].route[path[i+1]][j] + 1 + " ");
                        stw.WriteLine();
                    }
                    stw.WriteLine(dest[path[path.Count-1]].name);
                    int costsum = 0; for (int i = 0; i < path.Count - 1; i++) costsum += dest[path[i]].cost[path[i + 1]];
                    stw.WriteLine("TIME: " + costsum /6 +"h " + costsum % 6 * 10 + "m");
                }
            }
            catch(System.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
    }
}
