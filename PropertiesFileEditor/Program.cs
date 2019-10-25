
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace PropertiesFileEditor {
    class Program {
        static void Main( string[] args ) {
            PropertyParser propertyParser = new PropertyParser( args[ 0 ], args[ 1 ], args[ 2 ] );
            propertyParser.Parse();
        }
    }
}
