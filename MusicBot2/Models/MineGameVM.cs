using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBot2.Models
{
    public class MineGameVM
    {
        public int Width = 5;
        public int Height = 5;
        public int Mines = 5;

        public bool[,] MineMap;
        public bool[,] Revealed;
    }
}
