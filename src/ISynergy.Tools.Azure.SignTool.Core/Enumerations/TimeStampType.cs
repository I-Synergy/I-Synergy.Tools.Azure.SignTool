using System.Security.Cryptography;

namespace ISynergy.Tools.Azure.SignTool.Core.Enumerations;

/// <summary>
/// An enumeration of possible timestamp kinds.
/// </summary>
public enum TimeStampType
{
    /// <summary>
    /// Indicates that a timestamp authority should use the legacy Authenticode style of timestamps.
    /// This option should only be used for backward compatibility with Windows XP and only supports
    /// <see cref="HashAlgorithmName.SHA1" /> timestamp signatures.
    /// </summary>
    Authenticode,

    /// <summary>
    /// Indicates that a timestamp authority should use an RFC3161 timestamp signatures.
    /// </summary>
    RFC3161
}
