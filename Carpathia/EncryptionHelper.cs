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
using System.Text;

public class EncryptionHelper
{
    public string EncryptStringAsBase64(string plainText)
    {
        if (plainText == null)
            throw new ArgumentNullException(nameof(plainText));

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        byte[] encryptedBytes = ProtectedData.Protect(
            plainBytes,
            null, // No entropy
            DataProtectionScope.CurrentUser
        );

        return Convert.ToBase64String(encryptedBytes);
    }

    public string DecryptStringFromBase64(string base64CipherText)
    {
        if (string.IsNullOrWhiteSpace(base64CipherText))
            return string.Empty;

        byte[] encryptedBytes =
            Convert.FromBase64String(base64CipherText);

        byte[] decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null, // No entropy
            DataProtectionScope.CurrentUser
        );

        return Encoding.UTF8.GetString(decryptedBytes);
    }

}
