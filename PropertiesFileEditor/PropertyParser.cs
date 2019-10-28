using System;
using System.IO;
using System.Text;

namespace PropertiesFileEditor
{
    public class PropertyParser
    {
        private string _operation;
        private string _fileName;
        private string _property;
        private string TEMP_FILE = "tmpFile";
        private static string KEYWORD = "[SafeticaProperties]";
        private static int BUFFER_SIZE = 1024;
        private static int FOOTER_SIZE = 1024;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public PropertyParser( string operation, string fileName, string property ) {
            _operation = operation;
            _fileName = fileName;
            _property = property;
            TEMP_FILE += GetFileExtension( _fileName );
            OpenFiles();   
        }


        private void OpenFiles() {
            try {
                _writer = new BinaryWriter( new FileStream( TEMP_FILE, FileMode.OpenOrCreate ) );
                bool hasWritePermission = Tools.HasWritePermissionOnFile( TEMP_FILE );
                if ( !hasWritePermission ) throw new UnauthorizedAccessException();
                _reader = new BinaryReader( new FileStream( _fileName, FileMode.Open ) );
            }
            catch ( FileNotFoundException e ) {
                throw new FileNotFoundException( "File not found" + e.Message );
            }
            catch ( FileLoadException e ) {
                throw new FileLoadException( "File could NOT be loaded" + e.Message );
            }
            catch ( UnauthorizedAccessException e ) {
                throw new UnauthorizedAccessException( "Unauthorized access" + e.Message );
            }
            catch ( IOException e ) {
                throw new IOException( "IOException" + e.Message );
            }
            
        }
        /// <summary>
        /// Close the readers 
        /// </summary>
        private void Finalize() {
            _reader.Close();
            _writer.Close();
            SaveTheFile();
        }

        /// <summary>
        /// Deletes the old file and renames the temporary file into the new file
        /// </summary>
        private void SaveTheFile() {
            File.Delete( _fileName );
            File.Move( TEMP_FILE, _fileName );
        }

        public void ParseFile() {
            CopyOneFileToAnother();
            byte[] lastBytes = ReadLastBytes( BUFFER_SIZE );
            string footerString = Encoding.UTF8.GetString( lastBytes );

            int beginningIndex = footerString.IndexOf( "[SafeticaProperties]" );
            bool headerFound = beginningIndex != -1;

            Operation operation = new Operation();
            switch ( _operation ) {
                case "add": operation = Operation.ADD; break;
                case "edit": operation = Operation.EDIT; break;
                case "remove": operation = Operation.REMOVE; break;
            }

            if ( !headerFound ) {
                _writer.Write( lastBytes );
                if ( operation == Operation.ADD ) {
                    _writer.Write( Encoding.Default.GetBytes( createFooter() ) );
                }
                if ( operation == Operation.EDIT || operation == Operation.REMOVE ) {
                    Finalize();
                    return;
                }
            }
            else {
                string afterFooter = footerString.Substring( beginningIndex );
                string properties = afterFooter.Substring( KEYWORD.Length );
                string[] splitProperties = properties.Split( "\\n" );

                byte[] beforeFooterBytes = Tools.SubArray< byte >( lastBytes, 0, beginningIndex );
                _writer.Write( beforeFooterBytes );


                if ( operation == Operation.REMOVE ) {
                    RemoveProperty( splitProperties );
                }

                if ( operation == Operation.EDIT ) {
                    EditProperty( splitProperties );
                }

                if ( operation == Operation.ADD ) {
                    AddProperty( afterFooter );
                }
            }
            Finalize();
        }

        private void AddProperty( string afterFooter ) {
            byte[] newFooter = Encoding.Default.GetBytes( afterFooter + "\\n" + _property );
            if ( newFooter.Length <= FOOTER_SIZE ) {
                _writer.Write( newFooter );
            }
        }

        /// <summary>
        /// Iterates over properties and removes the given property
        /// </summary>
        /// <param name="splitProperties">String array of split properties</param>
        private void RemoveProperty( string[] splitProperties ) {
            string output = "[SafeticaProperties]";
            PropertyItem old = new PropertyItem( _property, "" );
            for ( int i = 1; i < splitProperties.Length; ++i ) { // 0th item is empty, so skip it
                PropertyItem item = SplitPropertyByEquals( splitProperties[ i ] );
                if ( old == item ) {
                    continue;
                }
                output += "\\n" + item.ToString();
            }
            byte[] afterFooter = Encoding.Default.GetBytes( output );
            _writer.Write( afterFooter );
        }

        /// <summary>
        /// Iterates over properties and edits the given property
        /// </summary>
        /// <param name="splitProperties">String array of split properties</param>
        private void EditProperty( string[] splitProperties ) {
            string output = "[SafeticaProperties]";
            PropertyItem newItem = new PropertyItem( _property );
            for ( int i = 1; i < splitProperties.Length; ++i ) {
                PropertyItem item = SplitPropertyByEquals( splitProperties[ i ] );
                if ( newItem == item ) {
                    item = newItem;
                }
                output += "\\n" + item.ToString();
            }
            byte[] afterFooter = Encoding.Default.GetBytes( output );
            _writer.Write( afterFooter );
        }

        /// <summary>
        /// Function copies the given file except the footer into temporary file.
        /// <para>If the size of file is less than/equal 1024B then we copy the whole file.</para>
        /// </summary>
        private void CopyOneFileToAnother() {
            _reader.BaseStream.Position = 0;
            byte[] buffer = new byte[ BUFFER_SIZE ];
            int remainingBytes = (int)new FileInfo( _fileName ).Length;
            if ( remainingBytes > BUFFER_SIZE ) {
                while ( remainingBytes >= 2 * BUFFER_SIZE ) { // skip junk
                    _reader.Read( buffer, 0, BUFFER_SIZE );
                    _writer.Write( buffer, 0, BUFFER_SIZE );
                    remainingBytes -= BUFFER_SIZE;
                }
                _reader.Read( buffer, 0, remainingBytes - BUFFER_SIZE );
                _writer.Write( buffer, 0, remainingBytes - BUFFER_SIZE );

                _reader.Read( buffer, 0, BUFFER_SIZE );
            }
            else {
                _reader.Read( buffer, 0, remainingBytes ); // read whole file
            }

        }

        /// <summary>
        /// Gets extension of file
        /// </summary>
        /// <param name="fileName">Name of file</param>
        /// <returns>File extension</returns>
        private string GetFileExtension( string fileName ) {
            int extensionIndex = fileName.IndexOf( '.' );
            return fileName.Substring( extensionIndex );

        }

        /// <summary>
        /// Reads last numberOfBytes bytes from the given file
        /// </summary>
        /// <param name="numberOfBytes">Number of bytes to read</param>
        /// <returns>Array with read bytes</returns>
        public byte[] ReadLastBytes( int numberOfBytes ) {
            int fileLength = (int)new FileInfo( _fileName ).Length;
            byte[] bytes;

            if ( fileLength >= numberOfBytes ) {
                // read last numberOfBytes bytes
                bytes = new byte[ numberOfBytes ];
                _reader.BaseStream.Seek( -numberOfBytes, SeekOrigin.End );
                _reader.Read( bytes, 0, numberOfBytes );
            }
            else {
                bytes = ReadWholeFile();
            }

            return bytes;
        }

        /// <summary>
        /// Reads whole file into byte array
        /// </summary>
        /// <returns></returns>
        private byte[] ReadWholeFile() {
            int fileLength = (int)new FileInfo( _fileName ).Length;
            byte[] bytes = new byte[ fileLength ];
            _reader.Read( bytes, 0, fileLength );
            return bytes;
        }

        /// <summary>
        /// Creates the footer
        /// </summary>
        /// <returns>Footer</returns>
        public string createFooter() {
            return "[SafeticaProperties]\\n" + _property;
        }

        /// <summary>
        /// Splits property into two parts: 1. Key 2. Value
        /// </summary>
        /// <param name="property">Property</param>
        /// <returns>PropertyItem </returns>
        public PropertyItem SplitPropertyByEquals( string property ) {
            int index = property.IndexOf( '=' );
            string key = property.Substring( 0, index );
            string value = property.Substring( index + 1 );
            return new PropertyItem( key, value );
        }

        /// <summary>
        /// Splits line into two parts:
        /// 1. Before header
        /// 2. After header
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Tuple of two parts</returns>
        private Tuple<string, string> SplitProperty( string str ) {
            int beginningIndex = str.IndexOf( "[SafeticaProperties]" );
            string beforeHeader = str.Substring( 0, beginningIndex );
            string afterHeader = str.Substring( beginningIndex );
            return new Tuple<string, string>( beforeHeader, afterHeader );
        }
    }
}
