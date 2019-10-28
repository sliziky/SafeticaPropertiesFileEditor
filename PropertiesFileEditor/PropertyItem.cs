using System;

namespace PropertiesFileEditor
{
    public class PropertyItem
    {
        public string Property { get; set; }
        public string Value { get; set; }
        public PropertyItem( string property, string value ) {
            Property = property;
            Value = value;
        }

        public PropertyItem( string property ) {
            string[] p = property.Split( "=" );
            Property = p[ 0 ];
            Value = p[ 1 ];
        }

        public override bool Equals( object obj ) {
            return this.Equals( obj as PropertyItem );
        }

        public bool Equals( PropertyItem item ) {
            if ( Object.ReferenceEquals( item, null ) ) {
                return false;
            }

            // Optimization for a common success case.
            if ( Object.ReferenceEquals( this, item ) ) {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if ( this.GetType() != item.GetType() ) {
                return false;
            }
            return Property.Equals( item.Property );
        }

        public override int GetHashCode() {
            return HashCode.Combine( Property, Value );
        }

        public override string ToString() {
            return Property + "=" + Value;
        }

        public static bool operator ==( PropertyItem lhs, PropertyItem rhs ) {
            // Check for null.
            if ( Object.ReferenceEquals( lhs, null ) ) {
                if ( Object.ReferenceEquals( rhs, null ) ) {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles the case of null on right side.
            return lhs.Equals( rhs );
        }

        public static bool operator !=( PropertyItem lhs, PropertyItem rhs ) {
            return !( lhs == rhs );
        }
    }
}
