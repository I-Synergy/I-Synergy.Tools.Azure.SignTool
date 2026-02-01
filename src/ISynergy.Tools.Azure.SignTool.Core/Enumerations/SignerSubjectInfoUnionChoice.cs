namespace ISynergy.Tools.Azure.SignTool.Core.Enumerations;

internal enum SignerSubjectInfoUnionChoice : uint
{
    SIGNER_SUBJECT_BLOB = 0x02,
    SIGNER_SUBJECT_FILE = 0x01
}