#nullable enable


namespace CodeFoxShop
{
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