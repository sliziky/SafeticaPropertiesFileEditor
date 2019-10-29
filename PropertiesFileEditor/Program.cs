
using System;
namespace PropertiesFileEditor
{
    static class Program
    {
        static void Main( string[] args ) {
            try {
                PropertyParser propertyParser = new PropertyParser( args[ 0 ], args[ 1 ], args[ 2 ] );
                propertyParser.StartParsing();
                propertyParser.Dispose();
            }
            catch( Exception e ) {
                Console.WriteLine( e );
            }
        }
    }
}
