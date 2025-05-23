﻿// Copyright 2021 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Samples.Managers;
using Esri.ArcGISRuntime.Mapping;
using Map = Esri.ArcGISRuntime.Mapping.Map;
#if __IOS__
using System.Security.Cryptography;
using System.Text;
#elif __ANDROID__ && !NETCOREAPP
using Microsoft.Maui.Storage.SecureStorage;
#else

using System.Security.Cryptography;
using System.Text;
#endif

namespace ArcGIS.Samples.Shared.Managers
{
    public static partial class ApiKeyManager
    {
        private static string _key;
        private static bool _keyDisabled;

        public static bool KeyDisabled
        {
            get { return _keyDisabled; }
        }

        internal static bool DisableApiKeyUI { get; private set; }

        // Name for file on windows systems. / Name for key in .NET MAUI SecureStorage.
        private const string _apiKeyFileName = "agolResource";

        public static string ArcGISDeveloperApiKey
        {
            get
            {
                // An API key is a unique long-lived access token that is used to authenticate and monitor requests to ArcGIS location services and private portal items.
                // You can create and manage an API key using your portal when you sign in with an ArcGIS Location Platform account or an ArcGIS Online account with administrator access or a custom role that has the Generate API keys privilege.
                // To learn how to create and manage API keys, go to the https://links.esri.com/create-an-api-key tutorial to create a new API key.
                // You must ensure that your API key has the correct privileges to access secure resources.
                return _key;
            }
            set
            {
                if (!_keyDisabled)
                {
                    // Set the environment key when the manager key is changed.
                    Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = _key = value;
                }
            }
        }

        public static void DisableKey()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = null;
            _keyDisabled = true;
        }

        public static void EnableKey()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = _key;
            _keyDisabled = false;
        }

        public static async Task<ApiKeyStatus> CheckKeyValidity()
        {
            try
            {
                // Check that a key has been set.
                if (string.IsNullOrEmpty(Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey))
                    return ApiKeyStatus.Missing;

                // Check that key is valid for loading a basemap.
                await new Map(BasemapStyle.ArcGISTopographic).LoadAsync();
                return ApiKeyStatus.Valid;
            }
            // An exception will be thrown when a Map using a BasemapStyle is created with an invalid API key.
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return ApiKeyStatus.Invalid;
            }
        }

        public static async Task<bool> TrySetLocalKey()
        {
            // Check for a local key if a key is not already set.
            if (ArcGISDeveloperApiKey == null)
            {
                try
                {
                    ArcGISDeveloperApiKey = await GetLocalKey();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            return false;
        }

        private static async Task<string> GetLocalKey()
        {
            // Uncomment the following line of code, and replace the string with your developer API key. Doing this here will work on all .NET sample viewers.
            // return "";
#if __IOS__
            try
            {
                return await Task.FromResult(
                    Encoding.Default.GetString(
                        Decrypt(
                            File.ReadAllBytes(
                                Path.Combine(DataManager.GetDataFolder(), _apiKeyFileName)
                            )
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
#elif ANDROID
            return await SecureStorage.GetAsync(_apiKeyFileName);
#else
            return await Task.FromResult(
                Encoding.Default.GetString(
                    Unprotect(
                        File.ReadAllBytes(
                            Path.Combine(DataManager.GetDataFolder(), _apiKeyFileName)
                        )
                    )
                )
            );
#endif
        }

        public static bool StoreCurrentKey()
        {
            try
            {
#if __IOS__
                File.WriteAllBytes(
                    Path.Combine(DataManager.GetDataFolder(), _apiKeyFileName),
                    Encrypt(
                        Encoding.Default.GetBytes(
                            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey
                        )
                    )
                );
                return true;
#elif ANDROID

                SecureStorage.SetAsync(
                    _apiKeyFileName,
                    Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey
                );
                return true;

#else

                File.WriteAllBytes(
                    Path.Combine(DataManager.GetDataFolder(), _apiKeyFileName),
                    Protect(
                        Encoding.Default.GetBytes(
                            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey
                        )
                    )
                );
                return true;

#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

#if !__IOS__ && !__ANDROID__

        #region Windows Data Protection

        private const int EntropySize = 16;

        // Generates a cryptographically random IV.
        private static byte[] GenerateEntropy()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var entropy = new byte[EntropySize];
                rng.GetBytes(entropy);
                return entropy;
            }
        }

        // Handles encrypting an array of bytes.
        private static byte[] Protect(byte[] bytes)
        {
            // Generate a random initialization vector.
            var entropy = GenerateEntropy();

            // Use the DPAPI to encrypt "bytes".
            var protectedBytes = ProtectedData.Protect(
                bytes,
                entropy,
                DataProtectionScope.CurrentUser
            );

            // Return the combined IV + protected data.
            var result = new byte[entropy.Length + protectedBytes.Length];
            System.Buffer.BlockCopy(entropy, 0, result, 0, entropy.Length);
            System.Buffer.BlockCopy(
                protectedBytes,
                0,
                result,
                entropy.Length,
                protectedBytes.Length
            );
            return result;
        }

        // Handles decrypting an array of bytes.
        private static byte[] Unprotect(byte[] bytes)
        {
            if (bytes?.Length < EntropySize)
                throw new InvalidDataException(); // data too small, likely invalid

            // Copy IV from "bytes".
            var entropy = new byte[EntropySize];
            System.Buffer.BlockCopy(bytes, 0, entropy, 0, entropy.Length);

            // Copy protected data.
            var protectedBytes = new byte[bytes.Length - EntropySize];
            System.Buffer.BlockCopy(bytes, EntropySize, protectedBytes, 0, protectedBytes.Length);

            // Return the unprotected data.
            return ProtectedData.Unprotect(
                protectedBytes,
                entropy,
                DataProtectionScope.CurrentUser
            );
        }

        #endregion Windows Data Protection

#endif

#if __IOS__

        #region iOS Data Protection

        // NOTE: Because iOS entitlements complicate provisioning, we are not using the iOS keychain in our .NET sample viewers. The keychain entitlement is required for .NET MAUI Secure Storage.
        // For production applications, we recommend using the Microsoft.Maui.Storage Secure Storage library. https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-7.0&tabs=ios

        // This is a randomly generated key used to encrypt ArcGIS API keys. Please replace this with your own randomly generated key.
        private const string iOSKey = "JHaq1bGgU1I9BVySrS33N8uNZxzo3Kug";

        // Handles encrypting an array of bytes.
        private static byte[] Encrypt(byte[] bytes)
        {
            // Create the encryptor.
            Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(iOSKey);
            aes.GenerateIV();
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            // Encrypt the input bytes.
            byte[] protectedBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            // Return the combined IV + protected data.
            var result = new byte[aes.IV.Length + protectedBytes.Length];
            System.Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            System.Buffer.BlockCopy(
                protectedBytes,
                0,
                result,
                aes.IV.Length,
                protectedBytes.Length
            );
            return result;
        }

        // Handles decrypting an array of bytes.
        private static byte[] Decrypt(byte[] bytes)
        {
            // Create the decryptor.
            Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(iOSKey);
            aes.GenerateIV();
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            // Decrypt the input bytes.
            byte[] decryptedBytes = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            // Copy the decrypted data.
            var decryptedData = new byte[decryptedBytes.Length - aes.IV.Length];
            System.Buffer.BlockCopy(
                decryptedBytes,
                aes.IV.Length,
                decryptedData,
                0,
                decryptedData.Length
            );

            // Return the decrypted data.
            return decryptedData;
        }

        #endregion iOS Data Protection

#endif
    }

    public enum ApiKeyStatus
    {
        Valid,
        Invalid,
        Missing
    }
}
