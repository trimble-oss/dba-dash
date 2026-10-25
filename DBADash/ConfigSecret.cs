using Microsoft.Data.SqlClient;
using System;

namespace DBADash
{
    /// <summary>
    /// A secret in the service config: the password of a SQL connection or the AWS secret key.  Used to list the secrets
    /// that can't be decrypted on this machine (e.g. after copying the config from another machine) and to set them.
    /// </summary>
    public class ConfigSecret
    {
        public enum SecretTypes
        {
            Destination,
            Source,
            AWSSecretKey
        }

        private readonly Action<string> setSecret;

        internal ConfigSecret(SecretTypes secretType, string name, DBADashConnection connection, string error, Action<string> setSecret)
        {
            SecretType = secretType;
            Name = name;
            Connection = connection;
            UserName = connection?.UserName;
            // A connection can log in with SQL authentication and a blank password - the AWS secret key is only listed when it's set
            HasValue = connection?.HasStoredPassword ?? true;
            Error = error;
            this.setSecret = setSecret;
        }

        internal static ConfigSecret ForConnection(SecretTypes secretType, string name, DBADashConnection connection) =>
            new(secretType, name, connection, connection.SecretError, connection.SetPassword);

        public SecretTypes SecretType { get; }

        /// <summary>ConnectionID for a source, server|database for a destination</summary>
        public string Name { get; }

        public string UserName { get; }

        /// <summary>Why the secret can't be decrypted.  Null if it can.</summary>
        public string Error { get; }

        public bool IsReadable => Error == null;

        /// <summary>False for a blank password</summary>
        public bool HasValue { get; }

        /// <summary>The connection the password belongs to.  Null for the AWS secret key.</summary>
        public DBADashConnection Connection { get; }

        public string TypeDescription => SecretType switch
        {
            SecretTypes.AWSSecretKey => "AWS secret key",
            _ => SecretType.ToString()
        };

        /// <summary>Replace the secret.  It's protected when it's set - save the config to keep it.</summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a protected value rather than the secret itself</exception>
        public void SetSecret(string value)
        {
            ThrowIfProtected(value);
            setSecret(value);
        }

        /// <summary>
        /// Throws if <paramref name="value"/> is a protected value (e.g. copied from a config file) rather than the secret
        /// itself.  It would be kept as it is, and might not be one this machine can decrypt.
        /// </summary>
        public static void ThrowIfProtected(string value)
        {
            if (SecretProtection.IsProtected(value))
            {
                throw new ArgumentException("Enter the password or secret itself, not a protected value copied from a config file.");
            }
        }

        /// <summary>Replace the secret with <see cref="SecretProtection.RemovedSecret"/>, for a config exported without secrets</summary>
        internal void Remove() => setSecret(SecretProtection.RemovedSecret);

        /// <summary>The connection string with <paramref name="password"/>, to test a password before setting it</summary>
        public string GetConnectionString(string password)
        {
            if (Connection == null) throw new InvalidOperationException("Not a SQL connection");
            return new SqlConnectionStringBuilder(Connection.ConnectionString) { Password = password }.ConnectionString;
        }

        public override string ToString() =>
            SecretType == SecretTypes.AWSSecretKey ? $"{TypeDescription}: {Error}" : $"{TypeDescription} {Name}: {Error}";
    }
}
