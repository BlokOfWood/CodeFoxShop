using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFoxShop
{
    public class Termek
    {
        public string Vonalkod { get; }
        public string Megnevezes { get; }
        public uint RaktarKeszlet { get; set; }
        public double BruttoEgysegar { get; }

        public Termek(string sor)
        {
            string[] bontott = sor.Split(';');
            Vonalkod = bontott[0];
            Megnevezes = bontott[1];
            RaktarKeszlet = uint.Parse(bontott[2]);

            //Ha nincs culture info, a jelenlegit veszi, ami magyar.
            //Ez azért baj, mert magyar szokás a vesszőt használni, pont helyett a tizedes törtekbe és a vesszőt keresi.
            BruttoEgysegar = double.Parse(bontott[3]);
        }

        public Termek(string _vonalkod, string _megnevezes, uint _raktarKeszlet, double _bruttoEgysegar)
        {
            Vonalkod = _vonalkod;
            Megnevezes = _megnevezes;
            RaktarKeszlet = _raktarKeszlet;
            BruttoEgysegar = _bruttoEgysegar;
        }

        public override string ToString()
        {
            return $"{Vonalkod};{Megnevezes};{RaktarKeszlet};{BruttoEgysegar}";
        }
    }
}
