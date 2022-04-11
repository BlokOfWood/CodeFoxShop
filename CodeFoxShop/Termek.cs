using System.Globalization;

namespace CodeFoxShop
{
    public class Termek
    {
        public double BruttoEgysegarErtek { get; set; }

        public string Vonalkod { get; set; }
        public string Megnevezes { get; set; }
        public uint RaktarKeszlet { get; set; }
        public string BruttoEgysegar 
        { 
            get => BruttoEgysegarErtek.ToString("C", new CultureInfo("hu-HU")); 
            set => BruttoEgysegarErtek = double.Parse(value); 
        }

        public Termek(string sor)
        {
            string[] bontott = sor.Split(';');
            Vonalkod = bontott[0];
            Megnevezes = bontott[1];
            RaktarKeszlet = uint.Parse(bontott[2]);

            //Ha nincs culture info, a jelenlegit veszi, ami magyar.
            //Ez azért baj, mert magyar szokás a vesszőt használni, pont helyett a tizedes törtekbe és a vesszőt keresi.
            BruttoEgysegarErtek = double.Parse(bontott[3]);
        }

        public Termek(string _vonalkod, string _megnevezes, uint _raktarKeszlet, double _bruttoEgysegar)
        {
            Vonalkod = _vonalkod;
            Megnevezes = _megnevezes;
            RaktarKeszlet = _raktarKeszlet;
            BruttoEgysegarErtek = _bruttoEgysegar;
        }

        public override string ToString()
        {
            return $"{Vonalkod};{Megnevezes};{RaktarKeszlet};{BruttoEgysegarErtek}";
        }
    }
}