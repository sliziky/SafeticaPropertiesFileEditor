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
        private string TEMP_FILE = "tmpFile";
        private static string KEYWORD = "[SafeticaProperties]";
        private static int BUFFER_VALUE = 1024;
        private string _readLine;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private FileStream stream;
        private byte[] _buffer = new byte[ BUFFER_VALUE ];
            

        public PropertyParser( string operation, string fileName, string property ) {
            _operation = operation;
            _fileName = fileName;
            _property = property;
            TEMP_FILE += GetFileExtension(fileName);
            _reader = new BinaryReader( new FileStream( _fileName, FileMode.OpenOrCreate ));
            stream = new FileStream(TEMP_FILE, FileMode.OpenOrCreate);
            _writer = new BinaryWriter( stream );
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
            //byte[] lastBytes = ReadLastBytes(1024);
            //string result = Convert.ToBase64String(lastBytes);
            //Console.WriteLine(result);
            ////doThis();
            //result += "[SafeticaProperties]\\nproperty1=value1";
            //byte[] data = Encoding.UTF8.GetBytes(result);
            //string b64 = Convert.ToBase64String(data);
            //lastBytes = Convert.FromBase64String(b64);
            //Open the existing file
            //Stream the existing file across
            CopyOneFileToAnother();
            byte[] footer = ReadLastBytes(1024);
           // _writer.Write(footer);
            string footerString = Encoding.UTF8.GetString(footer);
            
            int beginningIndex = footerString.IndexOf("[SafeticaProperties]");
            bool footerFound = beginningIndex != -1;

            // byte[] newArr = stringIntoBytes(footerString);

            if (!footerFound)
            {
                if (_operation.Equals("add"))
                {
                    _writer.Write(Encoding.Default.GetBytes(createFooter()));
                }
                if (_operation.Equals("edit") || _operation.Equals("remove"))
                {
                    Finalize();
                    return;
                }
            }
            else
            {
                string beforeFooterString = footerString.Substring(0, beginningIndex);
                string afterFooterString = footerString.Substring(beginningIndex);
                string properties = afterFooterString.Substring(KEYWORD.Length);
                string[] splitProperties = properties.Split("\\n");
                foreach (string s in splitProperties) {
                    Console.WriteLine(s);
                }

                string output = "[SafeticaProperties]";


                byte[] beforeFooter = Encoding.Default.GetBytes(beforeFooterString);
                _writer.Write(beforeFooter);


                if (_operation.Equals("remove"))
                {
                    PropertyItem old = new PropertyItem(_property, "");
                    for (int i = 1; i < splitProperties.Length; ++i)
                    {
                        PropertyItem item = SplitPropertyByEquals(splitProperties[i]);
                        if (old == item)
                        {
                            continue;
                        }
                        output += "\\n" + item.ToString();
                    }
                    byte[] afterFooter = Encoding.Default.GetBytes(output);
                    _writer.Write(afterFooter);
                }

                if (_operation.Equals("edit"))
                {
                    // REMOVE
                    PropertyItem newItem = new PropertyItem(_property);
                    for (int i = 1; i < splitProperties.Length; ++i)
                    {
                        PropertyItem item = SplitPropertyByEquals(splitProperties[i]);
                        if (newItem == item)
                        {
                            item = newItem; 
                        }
                        output += "\\n" + item.ToString();
                    }
                    byte[] afterFooter = Encoding.Default.GetBytes(output);
                    _writer.Write(afterFooter);
                }

                if (_operation.Equals("add"))
                {
                    // ADD
                    // have to cast it to byte[] because string contains some unwanted characters - dont know why
                    byte[] afterFooter = Encoding.Default.GetBytes(afterFooterString + "\\n" + _property);
                    _writer.Write(afterFooter);
                }
                
            }
            Finalize();
        }

        private byte[] stringIntoBytes(string str) {
            return new byte[str.Length];
        }
        private void Finalize() {
            _reader.Close();
            _writer.Close();
            //File.Delete( _fileName );
            //File.Move( TEMP_FILE, _fileName );
        }

        private void CopyOneFileToAnother() {
            _reader.BaseStream.Position = 0;
            byte[] buffer = new byte[ 1024 ];
            int remainingBytes = (int)new FileInfo( _fileName ).Length; 
            if (remainingBytes > 1024)
            {
                while (remainingBytes >= 2048)
                { // skip junk
                    _reader.Read(buffer, 0, 1024);
                    _writer.Write(buffer, 0, 1024);
                    remainingBytes -= 1024;
                }
                _reader.Read(buffer, 0, remainingBytes - 1024); // another junk
                _writer.Write(buffer, 0, remainingBytes - 1024);
                _reader.Read(buffer, 0, 1024); // most important -> this we will replace with properties
                //_writer.Write(buffer, 0, 1024);
            }
            else {
                _reader.Read(buffer, 0, remainingBytes);
            }
        }

        private string GetFileExtension(string fileName) {
            int extension = fileName.IndexOf('.');
            return fileName.Substring(extension);

        }
        private byte[] ReadLastBytes(int numberOfBytes) {
            _reader.BaseStream.Position = 0;
            int fileLength = (int)new FileInfo( _fileName ).Length;
            byte[] bytes;
            if ( fileLength >= numberOfBytes ) {
                 bytes = new byte[numberOfBytes];
                _reader.BaseStream.Seek( -numberOfBytes, SeekOrigin.End );
                _reader.Read( bytes, 0, numberOfBytes );
            }
            else {
                bytes = new byte[fileLength];
                _reader.Read(bytes, 0, fileLength);
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
