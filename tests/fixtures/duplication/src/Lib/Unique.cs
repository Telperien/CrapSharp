namespace Lib;

public static class Unique
{
    public static int Gcd(int a, int b)
    {
        while (b != 0)
            (a, b) = (b, a % b);
        return Math.Abs(a);
    }

    public static bool IsPalindrome(string text)
    {
        for (int i = 0, j = text.Length - 1; i < j; i++, j--)
            if (text[i] != text[j])
                return false;
        return true;
    }
}
