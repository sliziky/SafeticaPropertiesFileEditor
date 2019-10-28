using System;
using System.IO;
using System.Text;

namespace PropertiesFileEditor
{
    /// When speaking of header - we mean header of the footer
    /// 
    /// 
    public class PropertyParser
    {
        private Operation _operation;
        private string _fileName;
        private string _property;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public static string TEMP_FILE = "tmpFile";
        public static string KEYWORD = "[SafeticaProperties]";
        public static int BUFFER_SIZE = 1024;
        public static int MAX_FOOTER_SIZE = 1024;

        public PropertyParser( string operation, string fileName, string property ) {
            _operation = ParseOperation(operation);
            _fileName = fileName;
            _property = property;
            TEMP_FILE += GetFileExtension( _fileName );
            OpenFiles();   
        }


        private void OpenFiles() {
            try {
                _writer = new BinaryWriter( new FileStream( TEMP_FILE, FileMode.OpenOrCreate ) );
                // working only on Win
                // bool hasWritePermission = Tools.HasWritePermissionOnFile( TEMP_FILE );
                // if ( !hasWritePermission ) throw new UnauthorizedAccessException();
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
            CloseStreams();
            SaveTheFile();
        }

        private void CloseStreams() {
            _reader.Close();
            _writer.Close();
        }

        /// <summary>
        /// Deletes the old file and renames the temporary file into the new file
        /// </summary>
        private void SaveTheFile() {
            File.Delete( _fileName );
            File.Move( TEMP_FILE, _fileName );
        }

        private Operation ParseOperation(string operation) {
            return operation switch
            {
                "edit" => Operation.EDIT,
                "add" => Operation.ADD,
                "remove" => Operation.REMOVE,
                _ => Operation.ADD,
            };
        }

        /// <summary>
        /// Parses file
        /// </summary>
        /// First we recieve the last 1024 B or the whole file in bytes
        /// Then we split those bytes into 2 parts
        /// 1. Before header
        /// 2. After header
        /// Then we write back 'Before header' part
        /// And we parse the 'After header' part
        public void ParseFile() {
            CopyOneFileToAnother();


            byte[] lastBytes = ReadLastBytes( BUFFER_SIZE );
            string footerString = Encoding.UTF8.GetString( lastBytes );
            int beginningIndex = footerString.IndexOf( KEYWORD, StringComparison.Ordinal );
            bool headerFound = beginningIndex != -1;

            if ( !headerFound ) {
                _writer.Write( lastBytes );
                if ( _operation == Operation.ADD ) {
                    _writer.Write( Encoding.Default.GetBytes( CreateFooter() ) );
                }
            }
            else {
                byte[] beforeFooter = lastBytes.SubArray( 0, beginningIndex );
                _writer.Write( beforeFooter );

                string afterFooter = footerString.Substring( beginningIndex );
                string properties = afterFooter.Substring( KEYWORD.Length );
                string[] splitProperties = properties.Split( "\\n" );
                if ( _operation == Operation.REMOVE ) {
                    RemoveProperty( splitProperties );
                }

                if ( _operation == Operation.EDIT ) {
                    EditProperty( splitProperties );
                }

                if ( _operation == Operation.ADD ) {
                    AddProperty( afterFooter );
                }
            }
            Finalize();
        }
        
        private void AddProperty( string oldFooter ) {
            string newFooter = oldFooter + "\\n" + _property;
            WriteNewFooter( newFooter );
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
                    continue; // skip the property to be removed
                }
                output += "\\n" + item;
            }
            WriteNewFooter( output );
        }

        private void WriteNewFooter(string footer) {
            byte[] newFooter = Encoding.Default.GetBytes( footer );
            if( newFooter.Length <= MAX_FOOTER_SIZE ) {
                _writer.Write( newFooter );
            }
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
                    item = newItem; // edit the property to be editted
                }
                output += "\\n" + item;
            }
            WriteNewFooter( output );
        }

        /// <summary>
        /// Function copies the given file except the footer into temporary file.
        /// <para>If the size of file is less than/equal 1024 B then we copy the whole file.</para>
        /// </summary>
        /// We have to copy the whole file except for the last 1024 B
        /// 
        private void CopyOneFileToAnother() {
            
            byte[] buffer = new byte[ BUFFER_SIZE ];
            int remainingBytes = (int)new FileInfo( _fileName ).Length;
            if ( remainingBytes > BUFFER_SIZE ) {
                // read by chunks
                while ( remainingBytes >= 2 * BUFFER_SIZE ) { 
                    _reader.Read( buffer, 0, BUFFER_SIZE );
                    _writer.Write( buffer, 0, BUFFER_SIZE );
                    remainingBytes -= BUFFER_SIZE;
                }
                // here reaminingBytes are in range <1024, 2048>
                _reader.Read( buffer, 0, remainingBytes - BUFFER_SIZE );
                _writer.Write( buffer, 0, remainingBytes - BUFFER_SIZE );
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
            _reader.BaseStream.Position = 0;
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
        public string CreateFooter() {
            return "[SafeticaProperties]\\n" + _property;
        }

        /// <summary>
        /// Splits property into two parts: 1. Key 2. Value
        /// </summary>
        /// <param name="property">Property</param>
        /// <returns>PropertyItem </returns>
        public static PropertyItem SplitPropertyByEquals( string property ) {
            int index = property.IndexOf( '=' );
            string key = property.Substring( 0, index );
            string value = property.Substring( index + 1 );
            return new PropertyItem( key, value );
        }
    }
}
