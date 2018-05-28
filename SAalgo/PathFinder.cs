using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

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
        int width;int height;
        int nCross; //交差点の数 width*height 
        int[,] map; //グラフ（地図）データ
        Destination[] dest; //配達地点　並びはcsvの順
        List<int> path; //配達順
        int startpoint; //開始点/終着点
        int[] isDest; //その場所がDestかどうか(Destならその番号、違うなら-1)
        //const int culctime = 8; //計算時間制限(秒)
        const int writecount = 100; //SA中の報告頻度の設定
        string outdir;
        protected string outfile;
        bool finalReturn; //最後帰着するかどうか
        int seed = 334;
        const double T0 = 500; //500
        const double Tend = 0.01;
        double alpha = 0.9999;//Math.Pow(Tend / T0, 1/108000.0); //0.9999
        double alpha2 = (Tend - T0) / 108000.0;
        const int timesInTern = 10; //10
        //↓1-3 近傍状態の生成確率の比
        const double changeRatio = 1; //二者入れ替え
        const double insertRatio = 1; //単体移動
        const double reverseRatio = 1;//二者間逆順

        public PathFinder(int randomseed,int width,int height,bool finalReturn = true, string outDirectory = "result")
        {
            outdir = outDirectory;
            outfile = outdir + "\\betterpath.txt";
            seed = randomseed;
            this.width = width; this.height = height;
            this.finalReturn = finalReturn;
            nCross = width * height;
            map = new int[nCross, nCross];
            isDest = new int[nCross];
            for (int i =0;i<isDest.Length;i++) isDest[i] = -1;
            if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);
        }

        public int ex(string mapfile,string destsfile) //一連の計算を行う
        {
            ReadMap(mapfile);
            ReadDest(destsfile);
            findRoute();
            makePath(dest, startpoint, 1000 , finalReturn, out path);
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

        virtual protected void makePath(Destination[] dest,int startpoint,int wCap,bool SPreturn,out List<int> outpath)
        {
            List<int> path;
            

            //まず暫定解を作成する
            {
                var path1 = new List<int>();
                var path2 = new List<int>();
                var yetList = new List<int>();
                for (int i = 0; i < dest.Length; i++) if (i != startpoint) yetList.Add(i);
                path1.Add(startpoint);
                path2.Add(startpoint);
                for (int t = 1; t <= 2; t++)
                {
                    int cur = startpoint;
                    var tpath = (t == 1) ? path1 : path2;
                    var allsumweight = dest.Sum(d => d.weight);
                    while (tpath.Sum(i => dest[i].weight) <= allsumweight / 2 && yetList.Count > 0) 
                    {
                        int mincost = int.MaxValue; 
                        int mincostsp = int.MaxValue; 
                        int mindest = startpoint;
                        for (int i = 0; i < yetList.Count; i++)
                        {
                            var cost = dest[cur].cost[yetList[i]];
                            var costsp = dest[startpoint].cost[yetList[i]];
                            if (cost < mincost || cost == mincost && costsp < mincostsp)
                            { mincost = cost; mincostsp = costsp; mindest = yetList[i]; }
                        }
                        yetList.Remove(mindest);
                        tpath.Add(mindest);
                        cur = mindest;
                    }
                    if(SPreturn) tpath.Add(startpoint);
                }
                if(SPreturn) path1.RemoveAt(path1.Count - 1);
                path = path1; path1.AddRange(path2);
                for (int i = 0; i < path.Count; i++) Console.Write(dest[path[i]].name + "-");
                Console.WriteLine("\nTime:" + SumCost(path) * 10 + "min");
            }

            //SAを始める。
            Console.WriteLine("SA start.");
            var bestpath = new List<int>(path);
            int bestsum = SumCost(bestpath, 9 * 6);
            var T = T0;

            var numofnode = dest.Length;
            var random = new Random(seed);
            Int64 count = 0;
            try
            {
                using (var beststw = new System.IO.StreamWriter(outdir + "\\bestgraph.dat"))
                using (var curstw = new System.IO.StreamWriter(outdir + "\\curgraph.dat"))
                using (var thermostw = new System.IO.StreamWriter(outdir + "\\thermo.dat"))
                {
                    while (T > Tend)
                    {
                        var nextpath = new List<int>(path);

                        for (bool okeyflg = false; !okeyflg;) //合法でランダムな次状態を作成
                        {
                            //移動、互換、逆順のどれかをランダムに発生させる。
                            var dice = random.NextDouble();
                            if (dice > (insertRatio + reverseRatio) / (changeRatio + insertRatio + reverseRatio))
                            {
                                var fromNum = random.Next(1, numofnode);
                                var toNum = random.Next(1, numofnode);
                                int transdest = nextpath[fromNum];
                                nextpath[fromNum] = nextpath[toNum];
                                nextpath[toNum] = transdest;
                            }
                            else if (dice > reverseRatio / (changeRatio + insertRatio + reverseRatio))
                            {
                                do
                                {
                                    var fromNum = random.Next(1, numofnode + 1);
                                    var toNum = random.Next(1, numofnode + 1);
                                    int transdest = nextpath[fromNum];
                                    nextpath.RemoveAt(fromNum);
                                    nextpath.Insert(toNum, transdest);
                                } while (0.6 > random.NextDouble());

                            }
                            else
                            {
                                //順番の入れ替え
                                var fromNum = random.Next(1, numofnode + 1);
                                var toNum = random.Next(1, numofnode + 1);
                                nextpath.Reverse(Math.Min(fromNum, toNum), Math.Abs(fromNum - toNum));
                            }

                            if (MaxSumWeight(nextpath) <= wCap)
                                okeyflg = true;
                            else
                                nextpath = new List<int>(path);
                        }

                        var E1 = SumCost(nextpath);
                        var deltaE = E1 - SumCost(path);
                        if (deltaE < 0 || Math.Exp(-deltaE / T) > random.NextDouble())
                        {
                            if (E1 < bestsum)
                            {
                                bestsum = E1; bestpath = new List<int>(nextpath);
                            }
                            path = nextpath;
                        }

                        count++;
                        if (count % timesInTern == 0)
                            T *= alpha;//T += alpha2;

                        if (count % (writecount * 100) == 0) Console.WriteLine("count=" + count + " T=" + T + " cost:" + bestsum);
                        if (count % writecount == 0)
                        {
                            beststw.WriteLine(count + " " + bestsum);
                            curstw.WriteLine(count + " " + SumCost(path));
                            thermostw.WriteLine(count + " " + string.Format("{0:f3}", T));
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("SA finish. Count:" + count);

            outpath = bestpath;
        }

        int MaxSumWeight(List<int> path)
        {
            int maxsum = 0;
            int sum = 0;
            for(int i = 0; i < path.Count; i++)
            {
                sum += dest[path[i]].weight;
                maxsum = Math.Max(maxsum, sum);
                if (path[i] == startpoint)
                    sum = 0;
            }
            return maxsum;
        }

        int SumCost(List<int> path,int starttime=6*9) //timeはAM0:00を始点とし、10minで1単位であると定義する。
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
                    if (!dest[path[i]].PM)
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
                /*for(int i = 0; i < nCross; i++) //デバッグ用
                {
                    for (int j = 0; j < nCross; j++) System.Console.Write(map[i, j] + " ");
                    System.Console.Write('\n');
                }*/
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
                        var _weight = 50; var am = true; var pm = true;
                        if (nodes.Length > 2) _weight = int.Parse(nodes[2]);
                        if (nodes.Length > 3) { if (nodes[3] == "AM") pm = false; else if (nodes[3] == "PM") am = false; }
                        dList.Add(new Destination { name = nodes[0], pos = int.Parse(nodes[1]) - 1, weight = _weight, AM = am, PM = pm });
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
                    for (int i = 0; i < dest.Length; i++) Console.WriteLine("name:" + dest[i].name + " pos:" + dest[i].pos + " AM:" + dest[i].AM + " PM:" + dest[i].PM);
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
                    int costsum = SumCost(path);
                    stw.WriteLine("TIME: " + costsum /6 +"h " + costsum % 6 * 10 + "m");
                    stw.WriteLine("入替:移動:逆順 = " + changeRatio + ":" + insertRatio + ":" + reverseRatio);
                    stw.WriteLine("T0=" + T0 + ", alpha=" + alpha + ", 平衡時ループ数=" + timesInTern + ", Tend=" + Tend);
                    stw.WriteLine("randomseed=" + seed);
                }
            }
            catch(System.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
    }
}
