// LibreCanvas - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using Carpathia;
using System.Security.Cryptography;

public class EncryptionHelper
{
    // Changed from readonly to allow updating
    private static byte[] _key;
    private static byte[] _iv;

    // Add property accessors that always fetch the current values
    private static byte[] Key
    {
        get
        {
            // Only convert from Base64 if needed
            if (_key == null)
            {
                string keyBase64 = CryptographicSettings.Default.CipherKey;
                if (string.IsNullOrEmpty(keyBase64))
                {
                    throw new ArgumentException("Key 1 must be set in settings.");
                }
                _key = Convert.FromBase64String(keyBase64);
            }
            return _key;
        }
    }

    private static byte[] IV
    {
        get
        {
            // Only convert from Base64 if needed
            if (_iv == null)
            {
                string ivBase64 = CryptographicSettings.Default.CipherIV;
                if (string.IsNullOrEmpty(ivBase64))
                {
                    throw new ArgumentException("IV 1 must be set in settings.");
                }
                _iv = Convert.FromBase64String(ivBase64);
            }
            return _iv;
        }
    }

    public static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            // Validate key and IV sizes
            if (Key.Length != 16 && Key.Length != 24 && Key.Length != 32)
            {
                throw new ArgumentException("Invalid key size. Key must be 16, 24, or 32 bytes.");
            }

            if (IV.Length != 16)
            {
                throw new ArgumentException("Invalid IV size. IV must be 16 bytes.");
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Encryption 1 error: " + ex.Message, Logger.LogTypes.Error);
            throw;
        }
    }
    public static string DecryptString(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            // Validate key and IV sizes
            if (Key.Length != 16 && Key.Length != 24 && Key.Length != 32)
            {
                throw new ArgumentException("Invalid key 1 size. Key must be 16, 24, or 32 bytes.");
            }

            if (IV.Length != 16)
            {
                throw new ArgumentException("Invalid IV 1 size. IV must be 16 bytes.");
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Decryption 1 error: " + ex.Message, Logger.LogTypes.Error);
            throw;
        }
    }
    
    public static void ChangeKeyAndIV()
    {
        try
        {
            // Create new AES instance and generate a new key and IV
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateKey();
                aesAlg.GenerateIV();

                // Convert the new key and IV to base64 strings
                string newKeyBase64 = Convert.ToBase64String(aesAlg.Key);
                string newIVBase64 = Convert.ToBase64String(aesAlg.IV);

                // Store the new key and IV in settings
                CryptographicSettings.Default.CipherKey = newKeyBase64;
                CryptographicSettings.Default.CipherIV = newIVBase64;
                CryptographicSettings.Default.Save();

                // Reset the static fields to force them to be reloaded
                _key = null;
                _iv = null;
                Logger.Log("Encryption key and IV 1 changed successfully.", Logger.LogTypes.Info);
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Error changing encryption key and IV 1: " + ex.Message, Logger.LogTypes.Error);
            throw;
        }
    }
    
}
