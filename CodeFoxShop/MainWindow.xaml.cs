#nullable enable

using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System;

namespace CodeFoxShop
{
    public partial class MainWindow : Window
    {
        List<Termek> Termekek = new();
        List<VasarlasiTetel> VásárlásiTételek = new();
        List<BevetelezesiTetel> BevetelezesiTetelek = new();
        string jelenlegiFajl;

        public MainWindow()
        {
            InitializeComponent();
            TermekTablazat.ItemsSource = Termekek;

            //Ha nincs culture info meghatározva, a rendszerét veszi, ami magyar.
            //Ez azért baj, mert magyar szokás vesszőt használni a tizedes törtekbe, viszont a csv fájl pontokat használ.
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            //Beállítja a táblázatokat magyar nyelvűre, hogy tizedesvesszőt használjanak pont helyett.
            TermekTablazat.Language = System.Windows.Markup.XmlLanguage.GetLanguage("hu-HU");
            BevetelezesTablazat.Language = System.Windows.Markup.XmlLanguage.GetLanguage("hu-HU");
            VasarlasTetelekTablazat.Language = System.Windows.Markup.XmlLanguage.GetLanguage("hu-HU");
        }

        private void TermekImport(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fajlDialogus = new()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                AddExtension = true,
                DefaultExt = ".csv"
            };

            //Nullable string, nem lehet elhagyni a "== true"-t
            if (fajlDialogus.ShowDialog() == true)
            {
                jelenlegiFajl = fajlDialogus.FileName;
                Termekek = File.ReadAllLines(fajlDialogus.FileName).Skip(1).ToList()
                    .ConvertAll(x => new Termek(x));
                TermekTablazat.ItemsSource = Termekek;
            }
        }

        /*  ***********  */
        /*  TERMÉKLISTA  */
        /*  ***********  */

        private void TermekExportalas(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fajlDialogus = new()
            {
                AddExtension = true,
                DefaultExt = ".csv",
                Filter = "Táblázat (.csv)|*.csv"
            };

            if (fajlDialogus.ShowDialog() == true)
            {
                string[] kimenet = new string[Termekek.Count + 1];
                kimenet[0] = "Vonalkód;Megnevezés;Raktárkészlet;Egységár";

                for (int i = 1; i < Termekek.Count + 1; i++)
                {
                    kimenet[i] = Termekek[i - 1].ToString();
                }

                jelenlegiFajl = fajlDialogus.FileName;
                File.WriteAllLines(fajlDialogus.FileName, kimenet);
            }
        }

        private void TermekFelvetelGombNyomas(object sender, RoutedEventArgs e)
        {
            TabKezelo.SelectedIndex = 1;
        }

        private void TermekekTorlese(object sender, RoutedEventArgs e)
        {
            var eredmeny = MessageBox.Show("Biztos törölni akarod ezeket a tárgyakat a leltárból?", "Leltár elem törlés", MessageBoxButton.YesNo);

            if (eredmeny == MessageBoxResult.No)
                return;

            foreach(var i in TermekTablazat.SelectedItems)
            {
                Termekek.Remove((Termek)i);
            }
            TermekTablazat.Items.Refresh();
        }

        private void TermekTablazat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;
            e.Handled = true;

            TermekekTorlese(null, null);
        }

        /*  ******************************  */
        /*          TERMÉK FELVÉTEL         */
        /*  ******************************  */

        private void TermekFelvetelMezoUrites()
        {
            int kivalasztottIndex = TermekTablazat.SelectedIndex;
            VonalkodBox.Text = "";
            MegnevezesBox.Text = "";
            KeszletBox.Text = "";
            EgysegarBox.Text = "";
        }

        private void TermekFelvetele(object sender, RoutedEventArgs e)
        {
            if (VonalkodBox.Text == "" || MegnevezesBox.Text == "")
            {
                ErrorUzenet("Üres mező!", "Termék felvételi hiba");
                return;
            }

            uint? keszlet = MezoEllenorzes(KeszletBox.Text, Parserek.UINT, "készlet");
            double? egysegar = MezoEllenorzes(EgysegarBox.Text, Parserek.DOUBLE, "egységár");
            
            if (keszlet == null || egysegar == null)
                return;

            Termekek.Add(new Termek(VonalkodBox.Text, MegnevezesBox.Text, (uint)keszlet, (double)egysegar));
            TermekTablazat.Items.Refresh();

            TabKezelo.SelectedIndex = 0;
            TermekFelvetelMezoUrites();
        }

        private void VonalkodBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            MegnevezesBox.Focus();
        }

        private void MegnevezesBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            KeszletBox.Focus();
        }

        private void KeszletBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            EgysegarBox.Focus();
        }

        private void EgysegarBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            e.Handled = true;
            
            TermekFelvetele(null,null);
        }

        /*  ******************************  */
        /*              VÁSÁRLÁS            */
        /*  ******************************  */


        private void VasarlasMezoUrites(bool mezokEngedelyezve = true, bool tablazatForrasNullozva = false, bool osszegUres = false)
        {
            VasarlasTetelekTablazat.Items.Refresh();

            VetelVonalkodBox.Text = "";
            VasarlasMennyisegBox.Text = "";
            OsszegKiiras.Text = osszegUres ? "" : "0 Ft";

            VetelVonalkodBox.IsEnabled = mezokEngedelyezve;
            VasarlasMennyisegBox.IsEnabled = mezokEngedelyezve;

            if (tablazatForrasNullozva)
                VasarlasTetelekTablazat.ItemsSource = null;
            else
                VasarlasTetelekTablazat.ItemsSource = VásárlásiTételek;
        }

        private void UjVevo(object sender, RoutedEventArgs e)
        {
            VásárlásiTételek.Clear();
            VasarlasTetelekTablazat.ItemsSource = VásárlásiTételek;

            VasarlasMezoUrites();
        }

        private void VasarlasTetelFelvetele(object sender, RoutedEventArgs e)
        {

            if (VasarlasTetelekTablazat.ItemsSource == null)
            {
                MessageBox.Show("Nem lett megkezdve vásárlás!", "Felvételi hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Termek? kiválaszottTermék = Termekek.Find(x => x.Vonalkod == VetelVonalkodBox.Text);
            if (kiválaszottTermék == null)
            {
                MessageBox.Show("Nincs ilyen termék!", "Felvételi hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                VetelVonalkodBox.Text = "";
                return;
            }

            uint? mennyiseg = 1;
            if (VasarlasMennyisegBox.Text != "")
            {
                mennyiseg = MezoEllenorzes(VasarlasMennyisegBox.Text, Parserek.UINT, "mennyiség");
                if (mennyiseg == null) return;
            }

            int MeglévőIndex = VásárlásiTételek.FindIndex(x => x.Ugyanaz(kiválaszottTermék));

            if (MeglévőIndex != -1)
                VásárlásiTételek[MeglévőIndex].Mennyiseg += (uint)mennyiseg;
            else
                VásárlásiTételek.Add(new VasarlasiTetel(kiválaszottTermék, (uint)mennyiseg));

            VasarlasMezoUrites();
            OsszegKiiras.Text = VásárlásiTételek.Sum(x => x.OsszegAr) + " Ft";
        }

        private void VasarlasVonalkodBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VasarlasMennyisegBox.Focus();
                VasarlasMennyisegBox.Text = "1";
                VasarlasMennyisegBox.SelectAll();
            }
        }

        private void VasarlasBefejezese(object sender, RoutedEventArgs e)
        {
            foreach (VasarlasiTetel vasarlasiTetel in VásárlásiTételek)
            {
                int LeltarIndex = Termekek.FindIndex(y => vasarlasiTetel.Ugyanaz(y));

                Termekek[LeltarIndex].RaktarKeszlet -= vasarlasiTetel.Mennyiseg;
                TermekTablazat.Items.Refresh();
            }

            VasarlasMezoUrites(false, true, true);
        }

        private void VetelMennyisegBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VetelVonalkodBox.Focus();
                VasarlasTetelFelvetele(null, null);
            }
        }

        /*  ******************************  */
        /*             BEVÉTELEZÉS          */
        /*  ******************************  */

        private void BevetelezesMezoUrites(bool listaUritve = false, bool mezokEngedelyezve = true, bool tablazatForrasNullozva = false)
        {
            BevetelezesTablazat.Items.Refresh();

            BevetelezesVonalkod.Text = "";
            BevetelezesMennyiseg.Text = "";

            BevetelezesVonalkod.IsEnabled = mezokEngedelyezve;
            BevetelezesMennyiseg.IsEnabled = mezokEngedelyezve;

            if (tablazatForrasNullozva)
                BevetelezesTablazat.ItemsSource = null;
            else
                BevetelezesTablazat.ItemsSource = BevetelezesiTetelek;

            if (listaUritve)
                BevetelezesiTetelek.Clear();
        }

        private void BevetelezesTetelFelvetele(object sender, RoutedEventArgs e)
        {
            if (BevetelezesTablazat.ItemsSource == null)
            {
                ErrorUzenet("Nem lett megkezdve bevételezés!", "Bevételezés hiba");
                return;
            }

            Termek? kivalasztottTermek = Termekek.Find(x => x.Vonalkod == BevetelezesVonalkod.Text);
            if (kivalasztottTermek == null)
            {
                ErrorUzenet("Nincs ilyen termék!", "Bevételezés hiba");
                BevetelezesVonalkod.Text = "";
                return;
            }

            uint? mennyiseg = MezoEllenorzes(BevetelezesMennyiseg.Text, Parserek.UINT, "mennyiség");
            if (mennyiseg == null) return;

            int MeglevoIndex = BevetelezesiTetelek.FindIndex(x => x.Ugyanaz(kivalasztottTermek));
            if (MeglevoIndex == -1)
                BevetelezesiTetelek.Add(new BevetelezesiTetel(kivalasztottTermek, (uint)mennyiseg));
            else
                BevetelezesiTetelek[MeglevoIndex].Mennyiseg += (uint)mennyiseg;

            BevetelezesMezoUrites();
        }

        private void BevetelezesMentes(object sender, RoutedEventArgs e)
        {
            foreach(BevetelezesiTetel bevetelezesiTetel in BevetelezesiTetelek)
            { 
                int LeltarIndex = Termekek.FindIndex(y => bevetelezesiTetel.Ugyanaz(y));
                Termekek[LeltarIndex].RaktarKeszlet += bevetelezesiTetel.Mennyiseg;
                TermekTablazat.Items.Refresh();
            }

            BevetelezesMezoUrites(false, false, true);
        }

        private void BevetelezesMegse(object sender, RoutedEventArgs e) =>
            BevetelezesMezoUrites(true, false, true);
        

        private void BevetelezesUj(object sender, RoutedEventArgs e) =>
            BevetelezesMezoUrites(true);

        private void BevetelezesVonalkodKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BevetelezesMennyiseg.Focus();
                BevetelezesMennyiseg.Text = "1";
                BevetelezesMennyiseg.SelectAll();
            }
        }

        private void BevetelezesMennyiseg_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                BevetelezesTetelFelvetele(null, null);
                BevetelezesVonalkod.Focus();
            }
        }

        /*  ******************************  */
        /*          SEGÉD METÓDUSOK         */
        /*  ******************************  */

        public static T? MezoEllenorzes<T>(string ellenorzottSzoveg, Func<string, (bool sikeresség, T eredmény)> parseFunction, string mezoNeve) where T : struct
        {
            if (ellenorzottSzoveg.Length == 0)
            {
                ErrorUzenet($"Nincs megadva {mezoNeve}!", "Beviteli hiba");
                return null;
            }

            var (sikeresség, eredmény) = parseFunction(ellenorzottSzoveg);
            if (!sikeresség)
            {
                ErrorUzenet($"Hibásan megadott {mezoNeve} érték!", "Beviteli hiba");
                return null;
            }

            return eredmény;
        }

        public static void ErrorUzenet(string uzenet, string ablakCim) =>
            MessageBox.Show(uzenet, ablakCim, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}