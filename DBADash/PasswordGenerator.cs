using System;
using System.Security.Cryptography;
using System.Text;

namespace DBADash;

public static class PasswordGenerator
{
    private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Numbers = "1234567890";
    private const string SpecialCharacters = "!@#$%^&*()-_=+{}[]|:;<>,.?/";

    public static string Generate(
        int length = 20,
        bool includeUppercase = true,
        bool includeNumbers = true,
        bool includeSpecialCharacters = true)
    {
        if (length < 1)
            throw new ArgumentException("Password length must be at least 1.", nameof(length));

        var password = new StringBuilder();
        var characterSet = LowerCase;

        if (includeUppercase)
            characterSet += UpperCase;

        if (includeNumbers)
            characterSet += Numbers;

        if (includeSpecialCharacters)
            characterSet += SpecialCharacters;

        if (characterSet.Length == 0)
            throw new InvalidOperationException("No characters available for password generation.");

        var buffer = new byte[4];

        for (var i = 0; i < length; i++)
        {
            RandomNumberGenerator.Fill(buffer);
            var num = BitConverter.ToUInt32(buffer, 0);
            password.Append(characterSet[(int)(num % (uint)characterSet.Length)]);
        }

        return password.ToString();
    }
}