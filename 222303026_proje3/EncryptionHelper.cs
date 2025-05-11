using _222303026_proje3;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class EncryptionHelper
{
    // Changed from readonly to allow updating
    private static byte[] _key;
    private static byte[] _iv;

    // Add property accessors that always fetch the current values
    private static byte[] Key1
    {
        get
        {
            // Only convert from Base64 if needed
            if (_key == null)
            {
                string keyBase64 = CryptographicSettings.Default.CipherKey1;
                if (string.IsNullOrEmpty(keyBase64))
                {
                    throw new ArgumentException("Key 1 must be set in settings.");
                }
                _key = Convert.FromBase64String(keyBase64);
            }
            return _key;
        }
    }
    private static byte[] Key2
    {
        get
        {
            // Only convert from Base64 if needed
            if (_key == null)
            {
                string keyBase64 = CryptographicSettings.Default.CipherKey2;
                if (string.IsNullOrEmpty(keyBase64))
                {
                    throw new ArgumentException("Key 2 must be set in settings.");
                }
                _key = Convert.FromBase64String(keyBase64);
            }
            return _key;
        }
    }
    

    private static byte[] IV1
    {
        get
        {
            // Only convert from Base64 if needed
            if (_iv == null)
            {
                string ivBase64 = CryptographicSettings.Default.CipherIV1;
                if (string.IsNullOrEmpty(ivBase64))
                {
                    throw new ArgumentException("IV 1 must be set in settings.");
                }
                _iv = Convert.FromBase64String(ivBase64);
            }
            return _iv;
        }
    }
    private static byte[] IV2
    {
        get
        {
            // Only convert from Base64 if needed
            if (_iv == null)
            {
                string ivBase64 = CryptographicSettings.Default.CipherIV2;
                if (string.IsNullOrEmpty(ivBase64))
                {
                    throw new ArgumentException("IV 2 must be set in settings.");
                }
                _iv = Convert.FromBase64String(ivBase64);
            }
            return _iv;
        }
    }

    // Remove static constructor as we're using properties now

    public static string EncryptString1(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            // Validate key and IV sizes
            if (Key1.Length != 16 && Key1.Length != 24 && Key1.Length != 32)
            {
                throw new ArgumentException("Invalid key 1 size. Key must be 16, 24, or 32 bytes.");
            }

            if (IV1.Length != 16)
            {
                throw new ArgumentException("Invalid IV 1 size. IV must be 16 bytes.");
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key1;
                aesAlg.IV = IV1;

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
            System.Diagnostics.Debug.WriteLine("Encryption 1 error: " + ex.Message);
            throw;
        }
    }
    public static string EncryptString2(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            // Validate key and IV sizes
            if (Key2.Length != 16 && Key2.Length != 24 && Key2.Length != 32)
            {
                throw new ArgumentException("Invalid key 2 size. Key must be 16, 24, or 32 bytes.");
            }

            if (IV2.Length != 16)
            {
                throw new ArgumentException("Invalid IV 2 size. IV must be 16 bytes.");
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key2;
                aesAlg.IV = IV2;

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
            System.Diagnostics.Debug.WriteLine("Encryption 2 error: " + ex.Message);
            throw;
        }
    }
    public static string DecryptString1(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            // Validate key and IV sizes
            if (Key1.Length != 16 && Key1.Length != 24 && Key1.Length != 32)
            {
                throw new ArgumentException("Invalid key 1 size. Key must be 16, 24, or 32 bytes.");
            }

            if (IV1.Length != 16)
            {
                throw new ArgumentException("Invalid IV 1 size. IV must be 16 bytes.");
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key1;
                aesAlg.IV = IV1;

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
            System.Diagnostics.Debug.WriteLine("Decryption 1 error: " + ex.Message);
            throw;
        }
    }
    public static string DecryptString2(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            // Validate key and IV sizes
            if (Key2.Length != 16 && Key2.Length != 24 && Key2.Length != 32)
            {
                throw new ArgumentException("Invalid key 2 size. Key must be 16, 24, or 32 bytes.");
            }

            if (IV2.Length != 16)
            {
                throw new ArgumentException("Invalid IV 2 size. IV must be 16 bytes.");
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key2;
                aesAlg.IV = IV2;

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
            System.Diagnostics.Debug.WriteLine("Decryption 2 error: " + ex.Message);
            throw;
        }
    }
    public static void ChangeKeyAndIV1()
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
                CryptographicSettings.Default.CipherKey1 = newKeyBase64;
                CryptographicSettings.Default.CipherIV1 = newIVBase64;
                Settings1.Default.Save();

                // Reset the static fields to force them to be reloaded
                _key = null;
                _iv = null;

                System.Diagnostics.Debug.WriteLine("Generated new encryption key and IV 1");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error changing encryption key and IV 1: " + ex.Message);
            throw;
        }
    }
    public static void ChangeKeyAndIV2()
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
                CryptographicSettings.Default.CipherKey2 = newKeyBase64;
                CryptographicSettings.Default.CipherIV2 = newIVBase64;
                Settings1.Default.Save();

                // Reset the static fields to force them to be reloaded
                _key = null;
                _iv = null;

                System.Diagnostics.Debug.WriteLine("Generated new encryption key and IV 2");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error changing encryption key and IV 2: " + ex.Message);
            throw;
        }
    }
}
