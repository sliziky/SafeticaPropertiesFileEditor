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
        private string _readLine;
        private StreamWriter _writer;
        private StreamReader _reader;


        public PropertyParser( string operation, string fileName, string property ) {
            _operation = operation;
            _fileName = fileName;
            _property = property;
            _reader = new StreamReader( _fileName );
            _writer = new StreamWriter( TEMP_FILE, true );
        }



        public void Parse() {
            Operation operation = new Operation();
            if( _fileName.EndsWith( ".txt" ) ) {
                switch( _operation ) {
                    case "add": operation = Operation.ADD; break;
                    case "edit": operation = Operation.EDIT; break;
                    case "remove": operation = Operation.REMOVE; break;
                }
                ParseFile( operation );
            }
            Finalize();
        }

        private void Finalize() {
            File.Delete( _fileName );
            File.Move( TEMP_FILE, _fileName );
            _reader.Close();
            _writer.Close();
        }

        private void ParseFile( Operation operation ) {
            while( ( _readLine = _reader.ReadLine() ) != null ) {
                parseLine( operation );
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

                    if( _reader.EndOfStream ) {
                        _writer.WriteLine( "\\n" + _property );
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
                _writer.Write( propertyToBeEditted );
                return;
            }
            _writer.Write( propertyItem );
        }


        private void parseLine( Operation operation ) {
            if( _readLine.Contains( KEYWORD ) || _reader.EndOfStream ) {
                ParseLineContainingProperties( operation );
            }
            else {
                _writer.WriteLine( _readLine );
            }

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
