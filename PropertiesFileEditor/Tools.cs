using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace PropertiesFileEditor
{
    static class Tools
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static bool HasWritePermissionOnFile( string path ) {
            bool writeAllow = false;
            bool writeDeny = false;

            FileSecurity accessControlList = new FileInfo( path ).GetAccessControl();
            if ( accessControlList == null ) {
                return false;
            }

            var accessRules = accessControlList.GetAccessRules( true, true, typeof( SecurityIdentifier ) );
            if ( accessRules == null ) {
                return false;
            }

            foreach ( FileSystemAccessRule rule in accessRules ) {
                if ( ( FileSystemRights.Write & rule.FileSystemRights ) != FileSystemRights.Write ) {
                    continue;
                }

                if ( rule.AccessControlType == AccessControlType.Allow ) {
                    writeAllow = true;
                }
                else if ( rule.AccessControlType == AccessControlType.Deny ) {
                    writeDeny = true;
                }
            }

            return writeAllow && !writeDeny;
        }

    }
}
