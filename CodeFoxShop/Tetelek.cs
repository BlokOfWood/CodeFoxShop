using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFoxShop
{

    public class VasarlasiTetel
    {
        Termek termek;
        public string Megnevezes { get => termek.Megnevezes; }
        public double BruttoEgysegar { get => termek.BruttoEgysegar; }
        public uint Mennyiseg { get; set; }
        public double OsszegAr { get => termek.BruttoEgysegar * Mennyiseg; }

        public VasarlasiTetel(Termek _termek, uint _mennyiseg)
        {
            termek = _termek;
            Mennyiseg = _mennyiseg;
        }

        public bool Ugyanaz(Termek _termek)
            => termek.Vonalkod == _termek.Vonalkod;
    }

    public class BevetelezesiTetel
    {
        Termek termek;
        public string Megnevezes { get => termek.Megnevezes; }
        public uint Mennyiseg { get; set; }

        public BevetelezesiTetel(Termek _termek, uint _mennyiseg)
        {
            termek = _termek;
            Mennyiseg = _mennyiseg;
        }

        public bool Ugyanaz(Termek _termek)
            => termek.Vonalkod == _termek.Vonalkod;
    }
}
