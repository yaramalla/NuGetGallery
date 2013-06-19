﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage;

namespace NuGetGallery.Operations
{
    public class DeploymentEnvironment
    {
        public IDictionary<string, string> Settings { get; private set; } 
        public SqlConnectionStringBuilder MainDatabase { get; private set; }
        public SqlConnectionStringBuilder WarehouseDatabase { get; private set; }
        
        public CloudStorageAccount MainStorage { get; private set; }
        public CloudStorageAccount ReportStorage { get; private set; }
        public CloudStorageAccount BackupStorage { get; private set; }

        public Uri SqlDacEndpoint { get; private set; }

        public DeploymentEnvironment(IDictionary<string, string> deploymentSettings)
        {
            Settings = deploymentSettings;
            MainDatabase = GetSqlConnectionStringBuilder("Operations.Sql.Primary");
            WarehouseDatabase = GetSqlConnectionStringBuilder("Operations.Sql.Warehouse");

            MainStorage = GetCloudStorageAccount("Operations.Storage.Primary");
            BackupStorage = GetCloudStorageAccount("Operations.Storage.Backup");

            SqlDacEndpoint = Get("Operations.SqlDac", str => new Uri(str, UriKind.Absolute));
        }

        public static DeploymentEnvironment FromConfigFile(string configFile)
        {
            // Load the file
            var doc = XDocument.Load(configFile);

            // Build a dictionary of settings
            var settings = BuildSettingsDictionary(doc);

            // Construct the deployment environment
            return new DeploymentEnvironment(settings);
        }

        private string GetString(string key)
        {
            string value;
            if (!Settings.TryGetValue(key, out value))
            {
                return null;
            }
            return value;
        }

        private T Get<T>(string key, Func<string, T> thunk)
        {
            string val = GetString(key);
            return String.IsNullOrEmpty(val) ? default(T) : thunk(val);
        }

        private CloudStorageAccount GetCloudStorageAccount(string key)
        {
            return Get(key, str => CloudStorageAccount.Parse(str));
        }

        private SqlConnectionStringBuilder GetSqlConnectionStringBuilder(string key)
        {
            return Get(key, str => new SqlConnectionStringBuilder(str));
        }

        private static IDictionary<string, string> BuildSettingsDictionary(XDocument doc)
        {
            XNamespace ns = XNamespace.Get("http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration");
            return (from s in doc.Element(ns + "ServiceConfiguration")
                        .Element(ns + "Role")
                        .Element(ns + "ConfigurationSettings")
                        .Elements(ns + "Setting")
                    select new KeyValuePair<string, string>(
                        s.Attribute("name").Value,
                        s.Attribute("value").Value))
                    .ToDictionary(p => p.Key, p => p.Value);
        }
    }
}
