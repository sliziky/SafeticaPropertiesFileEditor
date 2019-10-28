using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;



namespace PropertiesFileEditor {
    public class PropertyParser {
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
            TEMP_FILE += GetFileExtension(fileName);
            _reader = new BinaryReader( new FileStream( _fileName, FileMode.OpenOrCreate ));
            _writer = new BinaryWriter(new FileStream(TEMP_FILE, FileMode.OpenOrCreate));
        }

        private void Finalize()
        {
            _reader.Close();
            _writer.Close();
            File.Delete(_fileName);
            File.Move(TEMP_FILE, _fileName);
        }

        public void Parse() {
            CopyOneFileToAnother();
            byte[] footer = ReadLastBytes(BUFFER_SIZE);
            string footerString = Encoding.UTF8.GetString(footer);
            
            int beginningIndex = footerString.IndexOf("[SafeticaProperties]");
            bool headerFound = beginningIndex != -1;

            if (!headerFound)
            {
                _writer.Write(footer);
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
                string afterFooterString = footerString.Substring(beginningIndex);
                string properties = afterFooterString.Substring(KEYWORD.Length);
                string[] splitProperties = properties.Split("\\n");

                byte[] beforeFooterBytes = Helper.SubArray<byte>(footer, 0, beginningIndex);
                _writer.Write(beforeFooterBytes);



                if (_operation.Equals("remove"))
                {
                    RemoveProperty(splitProperties);
                }

                if (_operation.Equals("edit"))
                {
                    EditProperty(splitProperties);
                }

                if (_operation.Equals("add"))
                {
                    byte[] afterFooter = Encoding.Default.GetBytes(afterFooterString + "\\n" + _property);
                    if (afterFooter.Length <= FOOTER_SIZE)
                    {
                        _writer.Write(afterFooter);
                    }

                }
            }
            Finalize();
        }

        private void AddProperty() { 
        }
        private void RemoveProperty(string[] splitProperties)
        {
            string output = "[SafeticaProperties]";
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

        private void EditProperty(string[] splitProperties)
        {
            string output = "[SafeticaProperties]";
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
           // Console.WriteLine(afterFooter.Length);
            _writer.Write(afterFooter);
        }

        private void CopyOneFileToAnother() {
            _reader.BaseStream.Position = 0;
            byte[] buffer = new byte[BUFFER_SIZE];
            int remainingBytes = (int)new FileInfo( _fileName ).Length; 
            if (remainingBytes > BUFFER_SIZE)
            {
                while (remainingBytes >= 2 * BUFFER_SIZE)
                { // skip junk
                    _reader.Read(buffer, 0, BUFFER_SIZE);
                    _writer.Write(buffer, 0, BUFFER_SIZE);
                    remainingBytes -= BUFFER_SIZE;
                }
                _reader.Read(buffer, 0, remainingBytes - BUFFER_SIZE);
                _writer.Write(buffer, 0, remainingBytes - BUFFER_SIZE);

                _reader.Read(buffer, 0, BUFFER_SIZE);
            }
            else {
                _reader.Read(buffer, 0, remainingBytes);
            }
        }

        private string GetFileExtension(string fileName) {
            int extension = fileName.IndexOf('.');
            return fileName.Substring(extension);

        }
        public byte[] ReadLastBytes(int numberOfBytes) {
            int fileLength = (int)new FileInfo( _fileName ).Length;
            byte[] bytes;
            if ( fileLength >= numberOfBytes ) {
                 bytes = new byte[numberOfBytes];
                _reader.BaseStream.Seek( -numberOfBytes, SeekOrigin.End );
                _reader.Read( bytes, 0, numberOfBytes );
            }
            else {
                _reader.BaseStream.Position = 0;
                bytes = new byte[fileLength];
                _reader.Read(bytes, 0, fileLength);
            }
            return bytes;
        }

        public string createFooter() {
            return "[SafeticaProperties]\\n" + _property;
        }

        public PropertyItem SplitPropertyByEquals( string property ) {
            int index = property.IndexOf( '=' );
            string key = property.Substring( 0, index );
            string value = property.Substring( index + 1 );
            return new PropertyItem( key, value );
        }

        private Tuple<string, string> SplitProperty(string str) {
            int beginningIndex = str.IndexOf("[SafeticaProperties]");
            string beforeFooterString = str.Substring(0, beginningIndex);
            string afterFooterString = str.Substring(beginningIndex);
            return new Tuple<string, string>(beforeFooterString, afterFooterString);
        }
    }
}
