#nullable enable

using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System;

namespace CodeFoxShop
{

    public partial class MainWindow : Window
    {
        List<Termek> Termekek = new List<Termek>();
        List<VasarlasiTetel> VásárlásiTételek = new List<VasarlasiTetel>();
        List<BevetelezesiTetel> BevetelezesiTetelek = new List<BevetelezesiTetel>();
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
            OpenFileDialog fajlDialogus = new OpenFileDialog
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

        private void TermekExportalas(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fajlDialogus = new SaveFileDialog
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

        private void TermekFelvetel(object sender, RoutedEventArgs e)
        {
            IndexBox.Text = "N/A";
            TabKezelo.SelectedIndex = 1;
        }

        private void TermekModositas(object sender, RoutedEventArgs e)
        {
            int kivalasztottIndex = TermekTablazat.SelectedIndex;
            VonalkodBox.Text = Termekek[kivalasztottIndex].Vonalkod;
            MegnevezesBox.Text = Termekek[kivalasztottIndex].Megnevezes;
            KeszletBox.Text = Termekek[kivalasztottIndex].RaktarKeszlet.ToString();
            EgysegarBox.Text = Termekek[kivalasztottIndex].BruttoEgysegar.ToString(CultureInfo.CreateSpecificCulture("en-US"));

            IndexBox.Text = (TermekTablazat.SelectedIndex + 1).ToString() + ".";
            TabKezelo.SelectedIndex = 1;
        }

        private void ListaModositas(object sender, RoutedEventArgs e)
        {
            if (VonalkodBox.Text == "" || MegnevezesBox.Text == "")
            {
                ErrorUzenet("Üres mező!", "Termék felvételi hiba");
                return;
            }

            uint? keszlet = MezoEllenorzes(KeszletBox.Text, Parserek.UINT, "készlet", "Lista módosítási hiba");
            double? egysegar = MezoEllenorzes(EgysegarBox.Text, Parserek.DOUBLE, "egységár", "Lista módosítási hiba");
            
            if (keszlet == null || egysegar == null)
                return;

            if (IndexBox.Text == "N/A")
            {
                Termekek.Add(new Termek(VonalkodBox.Text, MegnevezesBox.Text, (uint)keszlet, (double)egysegar));
            }
            else
            {
                int kiválaszottIndex = int.Parse(IndexBox.Text.Trim('.')) - 1;
                Termekek[kiválaszottIndex] = new Termek(VonalkodBox.Text, MegnevezesBox.Text, (uint)keszlet, (double)egysegar);
                IndexBox.Text = "N/A";
            }

            VonalkodBox.Text = "";
            MegnevezesBox.Text = "";
            KeszletBox.Text = "";
            EgysegarBox.Text = "";
            TermekTablazat.Items.Refresh();

            TabKezelo.SelectedIndex = 0;
        }

        private void UjVevo(object sender, RoutedEventArgs e)
        {
            VásárlásiTételek = new List<VasarlasiTetel>();
            VásárlásiTételek.Clear();
            VasarlasTetelekTablazat.ItemsSource = VásárlásiTételek;
            VetelVonalkodBox.IsEnabled = true;
            VetelMennyisegBox.IsEnabled = true;


            VetelVonalkodBox.Text = "";
            VetelMennyisegBox.Text = "";
            OsszegKiiras.Text = "0 Ft";
        }

        private void VasarlasTetelFelvetele(object sender, RoutedEventArgs e)
        {

            if (VasarlasTetelekTablazat.ItemsSource == null)
            {
                MessageBox.Show("Nem lett megkezdve vásárlás!", "Felvételi hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Termek kiválaszottTermék = Termekek.Find(x => x.Vonalkod == VetelVonalkodBox.Text);
            if (kiválaszottTermék == null)
            {
                MessageBox.Show("Nincs ilyen termék!", "Felvételi hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                VetelVonalkodBox.Text = "";
                return;
            }

            int MeglévőIndex = VásárlásiTételek.FindIndex(x => x.Ugyanaz(kiválaszottTermék));
            if (MeglévőIndex != -1)
            {
                if (VetelMennyisegBox.Text == "")
                {
                    VásárlásiTételek[MeglévőIndex].Mennyiseg += 1;
                }
                else
                {

                    uint mennyiseg = 0;
                    if (!uint.TryParse(VetelMennyisegBox.Text, out mennyiseg))
                    {
                        MessageBox.Show("Hibás mennyiség szám!", "Termék felvételi hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    VásárlásiTételek[MeglévőIndex].Mennyiseg += mennyiseg;
                }
            }
            else
            {
                if (VetelMennyisegBox.Text == "")
                {
                    VásárlásiTételek.Add(new VasarlasiTetel(kiválaszottTermék, 1));
                }
                else
                {

                    uint mennyiseg = 0;
                    if (!uint.TryParse(VetelMennyisegBox.Text, out mennyiseg))
                    {
                        MessageBox.Show("Hibás mennyiség szám!", "Termék felvételi hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (VetelMennyisegBox.Text != "")
                        VásárlásiTételek.Add(new VasarlasiTetel(kiválaszottTermék, mennyiseg));
                }
            }

            VasarlasTetelekTablazat.Items.Refresh();

            OsszegKiiras.Text = VásárlásiTételek.Sum(x => x.OsszegAr) + " Ft";

            VetelVonalkodBox.Text = "";
            VetelMennyisegBox.Text = "";
        }

        private void VetelVonalkodBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VetelMennyisegBox.Focus();
                VetelMennyisegBox.Text = "1";
                VetelMennyisegBox.SelectAll();
            }
        }

        private void VasarlasBefejezese(object sender, RoutedEventArgs e)
        {
            VásárlásiTételek.ForEach(x =>
            {
                int MeglévőIndex = Termekek.FindIndex(y => x.Ugyanaz(y));
                Termekek[MeglévőIndex].RaktarKeszlet -= x.Mennyiseg;
                TermekTablazat.Items.Refresh();
            });

            VetelVonalkodBox.IsEnabled = false;
            VetelMennyisegBox.IsEnabled = false;
            VasarlasTetelekTablazat.ItemsSource = null;
            VetelVonalkodBox.Text = "";
            VetelMennyisegBox.Text = "";
            OsszegKiiras.Text = "";
        }

        private void VetelMennyisegBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VetelVonalkodBox.Focus();
                VasarlasTetelFelvetele(null, null);
            }
        }

        private void BevetelezesMezoUrites(bool mezokEngedelyezve = true, bool tablazatForrasNullozva = false)
        {
            BevetelezesTablazat.Items.Refresh();

            BevetelezesVonalkod.Text = "";
            BevetelezesMennyiseg.Text = "";

            BevetelezesVonalkod.IsEnabled = mezokEngedelyezve;
            BevetelezesMennyiseg.IsEnabled = mezokEngedelyezve;

            if(tablazatForrasNullozva)
                BevetelezesTablazat.ItemsSource = null;
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
                MessageBox.Show("Nincs ilyen termék!", "Bevételezés hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                BevetelezesVonalkod.Text = "";
                return;
            }

            uint? mennyiseg = MezoEllenorzes(BevetelezesMennyiseg.Text, Parserek.UINT, "mennyiség", "Termék felvételi hiba");

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
            BevetelezesiTetelek.ForEach(x =>
            {
                int MeglévőIndex = Termekek.FindIndex(y => x.Ugyanaz(y));
                Termekek[MeglévőIndex].RaktarKeszlet += x.Mennyiseg;
                TermekTablazat.Items.Refresh();
            });

            BevetelezesMezoUrites(false, true);
        }

        private void BevetelezesMegse(object sender, RoutedEventArgs e)
        {
            BevetelezesiTetelek.Clear();

            BevetelezesMezoUrites(false, true);
        }

        private void BevetelezesUj(object sender, RoutedEventArgs e)
        {
            BevetelezesiTetelek.Clear();
            BevetelezesTablazat.ItemsSource = BevetelezesiTetelek;

            BevetelezesMezoUrites();
        }

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

        public static T? MezoEllenorzes<T>(string ellenorzottSzoveg, Func<string, (bool sikeresség, T eredmény)> parseFunction, string mezoNeve, string errorCím) where T : struct
        {
            if (ellenorzottSzoveg.Length == 0)
            {
                MessageBox.Show($"Nincs megadva {mezoNeve}!", errorCím, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            var parseEredmény = parseFunction(ellenorzottSzoveg);
            if (!parseEredmény.sikeresség)
            {
                MessageBox.Show($"Hibásan megadott {mezoNeve} érték!", errorCím, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return parseEredmény.eredmény;
        }

        public void ErrorUzenet(string uzenet, string ablakCim) =>
            MessageBox.Show(uzenet, ablakCim, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static class Parserek
    {
        public static (bool, uint) UINT(string s)
        {
            bool eredmény = uint.TryParse(s, out uint a);
            return (eredmény, a);
        }

        public static (bool, double) DOUBLE(string s)
        {
            bool eredmény = double.TryParse(s, out double a);
            return (eredmény, a);
        }
    }
}