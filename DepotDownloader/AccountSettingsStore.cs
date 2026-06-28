// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ProtoBuf;

namespace DepotDownloader
{
    [ProtoContract]
    class AccountSettingsStore
    {
        // Member 1 was a Dictionary<string, byte[]> for SentryData.

        [ProtoMember(2, IsRequired = false)]
        public ConcurrentDictionary<string, int> ContentServerPenalty { get; private set; }

        // Member 3 was a Dictionary<string, string> for LoginKeys.

        [ProtoMember(4, IsRequired = false)]
        public Dictionary<string, string> LoginTokens { get; private set; }

        [ProtoMember(5, IsRequired = false)]
        public Dictionary<string, string> GuardData { get; private set; }

        string FileName;

        AccountSettingsStore()
        {
            ContentServerPenalty = new ConcurrentDictionary<string, int>();
            LoginTokens = new(StringComparer.OrdinalIgnoreCase);
            GuardData = new(StringComparer.OrdinalIgnoreCase);
        }

        static bool Loaded
        {
            get { return Instance != null; }
        }

        public static AccountSettingsStore Instance;

        // The account store (containing the saved login/refresh token) is kept as
        // a plain file at the given path rather than in .NET isolated storage, so
        // the caller controls its location. OmniPacker points -config-dir at its
        // own data folder, which lets the token travel with portable installs and
        // keeps it out of the OS-specific isolated-storage hash directory.
        public static void LoadFromFile(string filename)
        {
            if (Loaded)
                throw new Exception("Config already loaded");

            if (File.Exists(filename))
            {
                try
                {
                    using var fs = File.Open(filename, FileMode.Open, FileAccess.Read);
                    using var ds = new DeflateStream(fs, CompressionMode.Decompress);
                    Instance = Serializer.Deserialize<AccountSettingsStore>(ds);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Failed to load account settings: {0}", ex.Message);
                    Instance = new AccountSettingsStore();
                }
            }
            else
            {
                Instance = new AccountSettingsStore();
            }

            Instance.FileName = filename;
        }

        public static void Save()
        {
            if (!Loaded)
                throw new Exception("Saved config before loading");

            try
            {
                var directory = Path.GetDirectoryName(Instance.FileName);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                using var fs = File.Open(Instance.FileName, FileMode.Create, FileAccess.Write);
                using var ds = new DeflateStream(fs, CompressionMode.Compress);
                Serializer.Serialize(ds, Instance);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Failed to save account settings: {0}", ex.Message);
            }
        }
    }
}
