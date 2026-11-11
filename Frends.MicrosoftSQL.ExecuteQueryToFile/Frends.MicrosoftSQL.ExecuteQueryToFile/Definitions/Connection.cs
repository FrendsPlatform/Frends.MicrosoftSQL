namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Attributes;

/// <summary>
/// Connection settings for the Microsoft SQL database.
/// </summary>
public class Connection
{
    /// <summary>
    /// Database connection string.
    /// </summary>
    /// <example>Server=myServerAddress;Database=myDataBase;User Id=myUsername;******;</example>
    [DefaultValue("\"Server=myServerAddress;Database=myDataBase;User Id=myUsername;******;\"")]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    [Required]
    public string ConnectionString { get; set; }

    /// <summary>
    /// If set, allows you to give the user credentials to use to make sql executions on remote hosts.
    /// If not set, the agent service user credentials will be used.
    /// This feature is only supported on Windows machines and requires ConnectionString to use Integrated Security=true.
    /// Connection pooling is disabled when this option is enabled.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool UseGivenUserCredentialsForRemoteConnections { get; set; }

    /// <summary>
    /// The username for the remote connection. Needs to be of format domain\username.
    /// </summary>
    /// <example>domain\username</example>
    [UIHint(nameof(UseGivenUserCredentialsForRemoteConnections), "", true)]
    [RequiredIf(nameof(UseGivenUserCredentialsForRemoteConnections), true)]
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("domain\\username")]
    public string Username { get; set; }

    /// <summary>
    /// Password for the given credentials.
    /// </summary>
    /// <example>Password123</example>
    [UIHint(nameof(UseGivenUserCredentialsForRemoteConnections), "", true)]
    [RequiredIf(nameof(UseGivenUserCredentialsForRemoteConnections), true)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string Password { get; set; }
}
