using System;

namespace Routing2
{
    class Program
    {
        const int width = 9; //地図の横幅
        const int height = 9; //地図の縦幅
        const string mapfile = "..\\..\\..\\..\\neighbor_mat.csv"; //読み込む地図のデータ.
        const string destfile = "..\\..\\..\\..\\destination.csv";  //読み込む目的地のデータ.
        static void Main(string[] args)//第一引数は出力ディレクトリ名 第二引数は乱数のシード値
        {
            bool finalReturn = true; //最後に帰着するか否か
            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            var pf = new PathFinder(int.Parse(args[1]), width, height, finalReturn, args[0]);
            pf.ex(mapfile, destfile);
        }
    }
}
