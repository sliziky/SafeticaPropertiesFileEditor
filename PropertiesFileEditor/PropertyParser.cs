using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertiesFileEditor {
    class PropertyParser {
        private string _operation;
        private string _fileName;
        private string _property;
        private static string TEMP_FILE = "tmpFile.txt";
        private static string KEYWORD = "[SafeticaProperties]";
        private static int BUFFER_VALUE = 1024;
        private string _readLine;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private byte[] _buffer = new byte[ BUFFER_VALUE ];


        public PropertyParser( string operation, string fileName, string property ) {
            _operation = operation;
            _fileName = fileName;
            _property = property;
            _reader = new BinaryReader( new FileStream( _fileName, FileMode.Open ));
            _writer = new BinaryWriter( File.Open( TEMP_FILE, FileMode.OpenOrCreate ) );
        }

        public void Parse() {
            //Operation operation = new Operation();
            //if( _fileName.EndsWith( ".txt" ) ) {
            //    switch( _operation ) {
            //        case "add": operation = Operation.ADD; break;
            //        case "edit": operation = Operation.EDIT; break;
            //        case "remove": operation = Operation.REMOVE; break;
            //    }
            //    ParseFile( operation );
            //}
            byte[] lastBytes = ReadLastBytes(1024);
            string result = Encoding.Default.GetString( lastBytes );
          //  Console.WriteLine( result );
            doThis();
            string newString = "This is new string";
            Finalize();
        }

        private void Finalize() {
            //File.Delete( _fileName );
            //File.Move( TEMP_FILE, _fileName );
            _reader.Close();
            _writer.Close();
        }

        private void doThis() {
            using( FileStream fileStream = new FileStream( _fileName, FileMode.Open, FileAccess.Read ) ) {
                byte[] buffer = new byte[ 1024 ];
                int read = 0;
                double fileLength = new FileInfo( _fileName ).Length;
                while( fileLength >= 2048 ) { // skip junk
                    fileStream.Read( buffer, 0, 1024 );
                    _writer.Write( Encoding.UTF8.GetString( buffer, 0, buffer.Length ) ) ;
                    fileLength -= 1024;
                }
                fileStream.Read( buffer, 0, (int)fileLength - 1024 ); // another junk
                fileStream.Read( buffer, 0, 1024 ); // most important -> this we will replace with properties
                Console.WriteLine( Encoding.UTF8.GetString( buffer, 0, buffer.Length ) );
            }
        }


        private byte[] ReadLastBytes(int numberOfBytes) {
            byte[] bytes = new byte[ numberOfBytes ];
            double fileLength = new FileInfo( _fileName ).Length;
            if( fileLength > numberOfBytes ) {
                _reader.BaseStream.Seek( -numberOfBytes, SeekOrigin.End );
                _reader.Read( bytes, 0, numberOfBytes );
            }
            else {
                bytes = File.ReadAllBytes( _fileName );
            }
            return bytes;
        }

        private void ParseFile( Operation operation ) {
            byte[] buffer = new byte[ BUFFER_VALUE ];
            using( var reader = new StreamReader( _fileName ) ) {
                if( reader.BaseStream.Length > 1024 ) {
                    var x = reader.BaseStream.Seek( -1024, SeekOrigin.End );
                }

                //reader.Read(buffer, )
                string line;
                while( ( line = reader.ReadLine() ) != null ) {
                    Console.WriteLine( line );
                }
            }
        }

        private void parseLine( Operation operation ) {
            if( _readLine.Contains( KEYWORD ) ) {
                ParseLineContainingProperties( operation );
            }
            else {
                _writer.Write( _buffer );
            }
        }

        private void ParseLineContainingProperties( Operation operation ) {
            Tuple<string, string> splitLine = SplitLineBeforeAfterHeader( _readLine );
            string beforeHeader = splitLine.Item1;
            string afterHeader = splitLine.Item2;
            _writer.Write( beforeHeader );
            _writer.Write( KEYWORD );

            afterHeader = RemoveBeginningNewlineChar( afterHeader );
            if( String.IsNullOrEmpty( afterHeader ) ) {
                if( operation == Operation.ADD ) {
                    _writer.Write( "\\n" + _property );
                }
                return;
            }

            IList<PropertyItem> properties = ParseProperties( afterHeader );
            foreach( PropertyItem propertyItem in properties ) {
                if( operation == Operation.REMOVE ) {
                    CheckForPropertyRemoval( propertyItem );
                }

                if( operation == Operation.EDIT ) {
                    CheckForPropertyEdit( propertyItem );
                }

                if( operation == Operation.ADD ) {

                    _writer.Write( "\\n" + afterHeader );

                    if( _buffer.Length < 1024 ) {
                        _writer.Write( _property );
                        return;
                    }
                }
            }
        }

        public void CheckForPropertyRemoval( PropertyItem propertyItem ) {
            PropertyItem propertyToBeRemoved = new PropertyItem( _property, "" );
            if( propertyItem == propertyToBeRemoved ) {
                return;
            }
            _writer.Write( "\\n" + propertyItem.Property );
        }

        public void CheckForPropertyEdit( PropertyItem propertyItem ) {
            PropertyItem propertyToBeEditted = SplitPropertyByEquals( _property );
            _writer.Write( "\\n" );
            if( propertyItem == propertyToBeEditted ) {
                _writer.Write( propertyToBeEditted.ToString() );
                return;
            }
            _writer.Write( propertyItem.ToString() );
        }





        public IList<PropertyItem> ParseProperties( string line ) {
            string[] properties = line.Split( "\\n" );
            IList<PropertyItem> splitProperties = new List<PropertyItem>();
            foreach( string property in properties ) {
                splitProperties.Add( SplitPropertyByEquals( property ) );
            }
            return splitProperties;

        }

        public string createFooter() {
            return "[SafeticaProperties]\\n" + _property;
        }


        public Tuple<string, string> SplitLineBeforeAfterHeader( string line ) {
            int index = line.IndexOf( KEYWORD );
            if( index == -1 ) {
                return new Tuple<string, string>( line, "" );
            }
            string beforeProperties = line.Substring( 0, index );
            string afterProperties = line.Substring( index + "[SafeticaProperties]".Length );
            return new Tuple<string, string>( beforeProperties, afterProperties );
        }

        public PropertyItem SplitPropertyByEquals( string property ) {
            int index = property.IndexOf( '=' );
            string key = property.Substring( 0, index );
            string value = property.Substring( index + 1 );
            return new PropertyItem( key, value );
        }


        public string RemoveBeginningNewlineChar( string str ) {
            if( str.Length >= 2 ) {
                return str.Substring( 2 );
            }
            else {
                return "";
            }
        }
    }
}
