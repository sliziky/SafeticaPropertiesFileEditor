
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace PropertiesFileEditor
{
    class Program
    {
        static string operation;
        static string fileName;
        static string property;
        static void Main(string[] args)
        {
            operation = args[0];
            fileName = args[1];
            property = "\\n" + args[2];
            Console.WriteLine("Operation " + operation);
            Console.WriteLine("FileName " + fileName);
            Console.WriteLine("Property: " + property);
 
            if (fileName.EndsWith(".txt")) {
                string path = @"C:\Users\Peter\Source\Repos\PropertiesFileEditor\PropertiesFileEditor\bin\Debug\netcoreapp3.0\" + fileName;
                string readLine = "";
                bool footerFound = false;
                string tempFile = "file.txt";
                bool add = true;
                using (StreamWriter writer = new StreamWriter(tempFile,true))
                using (StreamReader reader = new StreamReader(path))
                {
                    while ((readLine = reader.ReadLine()) != null)
                    {

                        if (readLine.Contains("[SafeticaProperties]"))
                        {
                            int index = readLine.IndexOf("[SafeticaProperties]");
                            int totalLength = readLine.Length;
                            string beforeProperties = readLine.Substring(0, index);
                            string afterProperties = readLine.Substring(index);
                            if (add && reader.EndOfStream ) {
                                afterProperties += property;
                            }
                            footerFound = true;
                            writer.Write(beforeProperties);
                            writer.WriteLine(afterProperties);
                        }
                        else {
                            if (!footerFound)
                            {
                                if (reader.EndOfStream)
                                {
                                    writer.Write(readLine);
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
                                else {
                                    writer.WriteLine(readLine);
                                }
                            }
                        }
                    }
                }
                using (StreamWriter writer = new StreamWriter(tempFile, true))
                {
                    if (!footerFound) {
                        writer.Write(createFooter());
                    }
                }

                    // if !footerFound -> create Footer
                    // else add to footer



                    Console.WriteLine(footerFound);


                //foreach (string line in lines)
                //{
                //    if (footerFound)
                //    {
                //        output += line;
                //    }
                //    if (line.Contains("[SafeticaProperties]")) {
                //        int index = line.IndexOf("[SafeticaProperties]");
                //        output += line.Substring(index);
                //        footerFound = true;
                //    }
                //}
                //output = output.Replace(@"\n", " "); // remove '\n'
                //Console.WriteLine(output);
                //string[] prps = output.Split(" ");
                //foreach (string p in prps) {
                //    Console.WriteLine("Property: " + p);
                //}

            }
        }
        public static string createFooter() {
            return "[SafeticaProperties]" + property;
        }
    }
}
