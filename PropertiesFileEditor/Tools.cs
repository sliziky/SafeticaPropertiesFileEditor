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
        /// <summary>
        /// Returns a subarray from the array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">Array</param>
        /// <param name="index">Position</param>
        /// <param name="length">How long subarray</param>
        /// <returns>Copy of original array</returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// https://stackoverflow.com/questions/9370509/the-morass-of-exceptions-related-to-opening-a-filestream
        /// <summary>
        /// Checks if we have a permission to write into the file
        /// Works only on Win
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <returns>Bool whether we have permission</returns>
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

            foreach( FileSystemAccessRule rule in accessRules ) {
                if( ( FileSystemRights.Write & rule.FileSystemRights ) != FileSystemRights.Write ) {
                    continue;
                }

                if( rule.AccessControlType == AccessControlType.Allow ) {
                    writeAllow = true;
                }
                else writeDeny |= rule.AccessControlType == AccessControlType.Deny;
            }
            return writeAllow && !writeDeny;
        }

    }
}
