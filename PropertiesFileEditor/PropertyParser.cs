using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertiesFileEditor
{
    class PropertyParser
    {
        private string operation;
        private string fileName;
        private string property;
        public PropertyParser(string operation, string fileName, string property)
        {
            this.operation = operation;
            this.fileName = fileName;
            this.property = property;
        }

        public void Parse() {
            if (fileName.EndsWith(".txt"))
            {
                switch (operation)
                {
                    case "add": AddProperty(property); break;
                    case "edit": EditProperty(property); break;
                    case "remove": RemoveProperty(property); break;
                }
            }
            finalize();
        }

        private void finalize() {
            File.Delete(fileName);
            File.Move("file.txt", fileName);
        }

        private void AddProperty(string property)
        {
            string path = @"C:\Users\Peter\Source\Repos\PropertiesFileEditor\PropertiesFileEditor\bin\Debug\netcoreapp3.0\" + fileName;
            string readLine = "";
            bool footerFound = false;
            const string tempFile = "file.txt";


            using StreamWriter writer = new StreamWriter(tempFile, true);
            using StreamReader reader = new StreamReader(path);
            while ((readLine = reader.ReadLine()) != null)
            {
             
                if (readLine.Contains("[SafeticaProperties]"))
                {
                    Tuple<string, string> splitLine = SplitLineBeforeAfterHeader(readLine);
                    string beforeHeader = splitLine.Item1;
                    string afterHeader = splitLine.Item2;
                    if (reader.EndOfStream)
                    {
                        afterHeader += "\\n" + property;
                    }
                    writer.Write(beforeHeader);
                    writer.Write("[SafeticaProperties]");
                    writer.WriteLine(afterHeader); 
                    footerFound = true;
                }
                else
                {
                    if (!footerFound)
                    {
                        if (reader.EndOfStream)
                        {
                            writer.Write(readLine + createFooter());
                        }
                        else
                        {
                            writer.WriteLine(readLine);
                        }
                    }
                    else
                    {
                        if (reader.EndOfStream)
                        {
                            writer.WriteLine(readLine + property);
                        }
                        else
                        {
                            writer.WriteLine(readLine);
                        }
                    }
                }
            }
        }
        private void EditProperty(string property)
        {
            string path = @"C:\Users\Peter\Source\Repos\PropertiesFileEditor\PropertiesFileEditor\bin\Debug\netcoreapp3.0\" + fileName;
            string readLine = "";
            bool footerFound = false;
            const string tempFile = "file.txt";


            using StreamWriter writer = new StreamWriter(tempFile, true);
            using StreamReader reader = new StreamReader(path);
            while ((readLine = reader.ReadLine()) != null)
            {
                if (readLine.Contains("[SafeticaProperties]"))
                {
                    Tuple<string, string> splitLine = SplitLineBeforeAfterHeader(readLine);
                    string beforeHeader = splitLine.Item1;
                    string afterHeader = splitLine.Item2.Substring(2);
                    string[] properties = afterHeader.Split("\\n");
                    writer.Write(beforeHeader + "[SafeticaProperties]");
                    foreach (string p in properties)
                    {
                        var s = SplitPropertyByEquals(p);
                        var s2 = SplitPropertyByEquals(property);
                        if (s.Item1.Equals(s2.Item1))
                        {
                            writer.Write("\\n" + property);
                            continue;
                        }
                        writer.Write("\\n" + p);
                    }


                }
                else
                {
                    writer.WriteLine(readLine);
                }
            }


        }

        private void RemoveProperty(string property) {
            string path = @"C:\Users\Peter\Source\Repos\PropertiesFileEditor\PropertiesFileEditor\bin\Debug\netcoreapp3.0\" + fileName;
            string readLine = "";
            bool footerFound = false;
            const string tempFile = "file.txt";


            using StreamWriter writer = new StreamWriter(tempFile, true);
            using StreamReader reader = new StreamReader(path);
            while ((readLine = reader.ReadLine()) != null) {
                if (readLine.Contains("[SafeticaProperties]"))
                {
                    Tuple<string, string> splitLine = SplitLineBeforeAfterHeader(readLine);
                    string beforeHeader = splitLine.Item1;
                    string afterHeader = splitLine.Item2;
                    string[] properties = afterHeader.Split("\\n");
                    writer.Write(beforeHeader + "[SafeticaProperties]");
                    if (afterHeader.Length < 2)
                    {
                        return;
                    }
                    foreach (string p in properties)
                    {
                        var s = SplitPropertyByEquals(p);
                        if (s.Item1.Equals(property))
                        {
                            continue;
                        }
                        writer.Write("\\n" + p);
                    }
                }
                else {
                    writer.WriteLine(readLine);
                }
            }
        }
        public string createFooter()
        {
            return "[SafeticaProperties]\\n" + property;
        }

        public Tuple<string, string> SplitLineBeforeAfterHeader(string line) {
            int index = line.IndexOf("[SafeticaProperties]");
            string beforeProperties = line.Substring(0, index);
            string afterProperties = line.Substring(index + "[SafeticaProperties]".Length);
            return new Tuple<string, string>(beforeProperties, afterProperties);
        }

        public Tuple<string, string> SplitPropertyByEquals(string property) {
            int index = property.IndexOf('=');
            string key = property.Substring(0, index);
            string value = property.Substring(index + 1);
            return new Tuple<string, string>(key, value);
        }
    }
}
