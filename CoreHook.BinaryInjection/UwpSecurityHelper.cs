using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace CoreHook.BinaryInjection;

public class UwpSecurityHelper
{

    /// <summary>
    /// Security Identifier representing ALL_APPLICATION_PACKAGES permission.
    /// </summary>
    private static readonly SecurityIdentifier AllAppPackagesSid = new SecurityIdentifier("S-1-15-2-1");
    
    /// <summary>
    /// Grant ALL_APPLICATION_PACKAGES permissions to binary
    /// and configuration files in <paramref name="directoryPath"/>.
    /// </summary>
    /// <param name="directoryPath">Directory containing application files.</param>
    public static void GrantAllAppPackagesAccessToDir(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        GrantAllAppPackagesAccessToFolder(directoryPath);

        foreach (var folder in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            GrantAllAppPackagesAccessToFolder(folder);
        }

        foreach (var filePath in Directory.GetFiles(directoryPath, "*.json|*.dll|*.pdb", SearchOption.AllDirectories))
        {
            GrantAllAppPackagesAccessToFile(filePath);
        }
    }


    /// <summary>
    /// Grant ALL_APPLICATION_PACKAGES permissions to a directory at <paramref name="folderPath"/>.
    /// </summary>
    /// <param name="folderPath">The directory to be granted ALL_APPLICATION_PACKAGES permissions.</param>
    public static void GrantAllAppPackagesAccessToFolder(string folderPath)
    {
        try
        {
            var dirInfo = new DirectoryInfo(folderPath);

            DirectorySecurity acl = dirInfo.GetAccessControl(AccessControlSections.Access);
            acl.SetAccessRule(new FileSystemAccessRule(AllAppPackagesSid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));

            dirInfo.SetAccessControl(acl);
        }
        catch
        {
        }
    }


    /// <summary>
    /// Grant ALL_APPLICATION_PACKAGES permissions to a file at <paramref name="fileName"/>.
    /// </summary>
    /// <param name="fileName">The file to be granted ALL_APPLICATION_PACKAGES permissions.</param>
    public static void GrantAllAppPackagesAccessToFile(string fileName)
    {
        try
        {
            var fileInfo = new FileInfo(fileName);

            FileSecurity acl = fileInfo.GetAccessControl();
            acl.SetAccessRule(new FileSystemAccessRule(AllAppPackagesSid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));

            fileInfo.SetAccessControl(acl);
        }
        catch
        {
        }
    }
}
