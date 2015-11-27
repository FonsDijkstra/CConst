using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FonsDijkstra.CConst.Test
{
    class Iets
    {
        public int a;
    }

    class SyntaxVisualizer
    {
        private int i;

        int J { get; set; }

        public SyntaxVisualizer()
        {
            int k;
            k = 1;

            i = 0;
            J = 0;

            i = new Iets { a = 1, }.a;
        }
    }
}
