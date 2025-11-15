namespace WebApplication1.Constants.ControllersConstants;

public static class LabFourControllerConstants
{
    static LabFourControllerConstants();
    
    public static readonly string EntyIsOutsideOfTheTarget = "Entry is outside of the target directory";
    public static readonly string PublicKeyNotFount = "Public key file not found";
    public static readonly string ErrorLoadingPublic = "Error loading public key";
    public static readonly string FileIsRequired = "File is required";
    public static readonly string KeyFileNotFound = "Key file not found";
    public static readonly string OneOfKeysMustBeProvided = "Either privateKeyFilename or privateKeyFile must be provided";
    public static readonly string PemFormat = "*.pem";
    public static readonly string ErrorDecryptingFile = "Error decrypting file";
    public static readonly string ErrorEncryptingFile = "Error encrypting file";
}