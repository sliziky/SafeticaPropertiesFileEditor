using System;
using System.IO;
using System.Text;


namespace PropertiesFileEditor {
    /// The main logic of parsing footer is :
    /// a. Try to read the last 1024 B of the file, since we know that the footer can't be longer
    /// b. Find the header in those 1024 B and split these bytes into 2 parts - 1. before header, 2. after header
    /// c. Write "before header" part back into the file
    /// d. Parse the "after header" part
    /// e. Split the footer into the array of strings in form of "properties=value"
    /// d. Iterate through this array and do the operation corresponding to the input
    ///
    /// I used a testScript.sh to do basic testing while refactoring
    public class PropertyParser : IDisposable {
        private readonly Operation _operation;
        private readonly string _fileName;
        private readonly PropertyItem _propertyItem;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        private static string TempFile = "tmpFile";
        private const string Header = "[SafeticaProperties]";
        private const int BufferSize = 1024;
        private const int MaxFooterSize = 1024;

        public PropertyParser( string operation, string fileName, string property ) {
            _operation = ParseOperation( operation );
            _fileName = fileName;
            _propertyItem = ParseProperty( property );
            TempFile += GetFileExtension( _fileName );
            OpenFiles();
        }

        /// <summary>
        /// Parses operation
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        private Operation ParseOperation( string operation ) {
            return operation switch
            {
                "edit" => Operation.EDIT,
                "add" => Operation.ADD,
                "remove" => Operation.REMOVE,
                _ => Operation.ADD,
            };
        }

        /// <summary>
        /// Parses given property. If a value is missing, then the value is empty string.
        /// </summary>
        /// <param name="property">Given property</param>
        /// <returns>PropertyItem </returns>
        private PropertyItem ParseProperty( string property ) {
            return SplitPropertyByEquals( property );
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
        /// Open files and throws an exception if some occurs
        /// </summary>
        private void OpenFiles() {
            try {
                _writer = new BinaryWriter( new FileStream( TempFile, FileMode.OpenOrCreate ) );
                _reader = new BinaryReader( new FileStream( _fileName, FileMode.Open ) );
            }
            catch( FileNotFoundException e ) {
                throw new FileNotFoundException( "File not found" + e.Message );
            }
            catch( FileLoadException e ) {
                throw new FileLoadException( "File could NOT be loaded" + e.Message );
            }
            catch( UnauthorizedAccessException e ) {
                throw new UnauthorizedAccessException( "Unauthorized access" + e.Message );
            }
            catch( IOException e ) {
                throw new IOException( "IOException" + e.Message );
            }
        }

        /// <summary>
        /// Starts parsing given file
        /// </summary>
        public void StartParsing() {
            CopyOneFileToAnother();
            ParseFile();
            SaveTheFile();
        }


        /// <summary>
        /// Function copies the given file except the footer into temporary file.
        /// <para>If the size of file is less than/equal 1024 B then we copy the whole file.</para>
        /// </summary>
        /// We have to copy the whole file except for the last 1024 B
        /// 
        private void CopyOneFileToAnother() {

            byte[] buffer = new byte[ BufferSize ];
            int remainingBytes = ( int )new FileInfo( _fileName ).Length;
            if( remainingBytes > BufferSize ) {
                // read by chunks
                while( remainingBytes >= 2 * BufferSize ) {
                    _reader.Read( buffer, 0, BufferSize );
                    _writer.Write( buffer, 0, BufferSize );
                    remainingBytes -= BufferSize;
                }
                // here reaminingBytes are in range <1024, 2048>
                _reader.Read( buffer, 0, remainingBytes - BufferSize );
                _writer.Write( buffer, 0, remainingBytes - BufferSize );
            }
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
        private void ParseFile() {

            byte[] lastBytes = ReadLastBytes( BufferSize );
            string footerString = Encoding.Default.GetString( lastBytes );
            int beginningIndex = footerString.IndexOf( Header );
            bool headerFound = beginningIndex != -1;

            if( !headerFound ) {
                _writer.Write( lastBytes );
                if( _operation == Operation.ADD ) {
                    _writer.Write( Encoding.Default.GetBytes( CreateHeader() ) );
                }
            }
            else {
                byte[] beforeFooter = lastBytes.SubArray( 0, beginningIndex );
                _writer.Write( beforeFooter );
                BuildNewFooter( footerString );
            }
        }

        /// <summary>
        /// Deletes the old file and renames the temporary file into the new file
        /// </summary>
        private void SaveTheFile() {
            File.Delete( _fileName );
            File.Move( TempFile, _fileName );
        }

        /// <summary>
        /// Builds new footer
        /// </summary>
        /// <param name="footer"></param>
        private void BuildNewFooter( string footer ) {

            string[] splitProperties = SplitProperties( footer );
            string newFooter = Header;
            for( int i = 1; i < splitProperties.Length; ++i ) {
                PropertyItem item = SplitPropertyByEquals( splitProperties[ i ] );
                if( _propertyItem == item ) {
                    if( _operation == Operation.EDIT ) {
                        item = _propertyItem; // edit the property to be editted
                    }
                    if( _operation == Operation.REMOVE ) {
                        continue;
                    }
                }
                newFooter += "\\n" + item;
            }
            if( _operation == Operation.ADD ) {
                AppendProperty( ref newFooter );
            }
            WriteFooter( newFooter );
        }

        /// <summary>
        /// Writes footer into the file
        /// </summary>
        /// <param name="footer"></param>
        private void WriteFooter( string footer ) {
            byte[] newFooter = Encoding.Default.GetBytes( footer );
            if( newFooter.Length <= MaxFooterSize ) {
                _writer.Write( newFooter );
            }
        }

        /// <summary>
        /// Appends property into footer
        /// </summary>
        /// <param name="footer"></param>
        private void AppendProperty( ref string footer ) {
            footer += "\\n" + _propertyItem;
        }

        /// <summary>
        /// Reads last numberOfBytes bytes from the given file
        /// </summary>
        /// <param name="numberOfBytes">Number of bytes to read</param>
        /// <returns>Array with read bytes</returns>
        private byte[] ReadLastBytes( int numberOfBytes ) {

            int fileLength = ( int )new FileInfo( _fileName ).Length;
            byte[] bytes;
            if( fileLength >= numberOfBytes ) {
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
            _reader.BaseStream.Position = 0;
            int fileLength = ( int )new FileInfo( _fileName ).Length;
            byte[] bytes = new byte[ fileLength ];
            _reader.Read( bytes, 0, fileLength );
            return bytes;
        }

        /// <summary>
        /// Creates the footer
        /// </summary>
        /// <returns>Footer</returns>
        private string CreateHeader() {
            return Header + "\\n" + _propertyItem;
        }

        /// <summary>
        /// Splits property into two parts: 1. Key 2. Value
        /// </summary>
        /// <param name="property">Property</param>
        /// <returns>PropertyItem </returns>
        private static PropertyItem SplitPropertyByEquals( string property ) {
            int index = property.IndexOf( '=' );
            if( index == -1 ) {
                return new PropertyItem( property, "" );
            }
            string key = property.Substring( 0, index );
            string value = property.Substring( index + 1 );
            return new PropertyItem( key, value );
        }

        /// <summary>
        /// Splits footer into properties
        /// </summary>
        /// <param name="footer"></param>
        /// <returns></returns>
        private string[] SplitProperties(string footer) {
            int beginningIndex = footer.IndexOf( Header );
            string afterFooter = footer.Substring( beginningIndex );
            string properties = afterFooter.Substring( Header.Length );
            return properties.Split( "\\n" );
        }

        protected virtual void Dispose( bool disposing ) {
            if( disposing ) {
                _reader.Close();
                _writer.Close();
            }
        }
        public void Dispose() {
            Dispose( true );
            GC.SuppressFinalize( this );
        }
    }
}
