#!/bin/bash
file=testFile.dll
rm testFile.dll
cp PropertiesFileEditor.dll testFile.dll

./PropertiesFileEditor remove testFile.dll property1
echo REMOVE WITHOUT FOOTER
original_size=$(wc -c < "$file")
DIFF=$(diff PropertiesFileEditor.dll testFile.dll)
if [ "$DIFF" == "" ]; then
    echo Good
else
    echo Wrong
fi
./PropertiesFileEditor edit testFile.dll property1=value1
echo EDIT WITHOUT FOOTER
DIFF=$(diff PropertiesFileEditor.dll testFile.dll)
if [ "$DIFF" == "" ]; then
    echo Good
else
    echo Wrong
fi

./PropertiesFileEditor add testFile.dll property1=value1
echo ADDING property1=value1 WITHOUT FOOTER
size=$(wc -c < "$file")
new_size=$(($original_size + 38))
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi
new_size=$(($new_size - 4))
./PropertiesFileEditor edit testFile.dll property1=v1
echo EDITTING property1=v1 
size=$(wc -c < "$file")
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi

new_size=$(($new_size - 14))
./PropertiesFileEditor remove testFile.dll property1
echo REMOVING property1
size=$(wc -c < "$file")
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi


./PropertiesFileEditor add testFile.dll property1=value1
echo ADDING property1=value1
size=$(wc -c < "$file")
new_size=$(($new_size + 18))
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi


./PropertiesFileEditor add testFile.dll property2=value2
echo ADDING property2=value2
size=$(wc -c < "$file")
new_size=$(($new_size + 18))
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi

./PropertiesFileEditor remove testFile.dll property3
echo REMOVING NON-EXISTING PROP
size=$(wc -c < "$file")
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi

./PropertiesFileEditor edit testFile.dll property3=value3
echo EDITING NON-EXISTING PROP
size=$(wc -c < "$file")
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi


./PropertiesFileEditor edit testFile.dll property2=newvalue2
echo EDITING property2=newvalue2
size=$(wc -c < "$file")
new_size=$(($new_size + 3))
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi

new_size=$(($new_size - 18))
./PropertiesFileEditor remove testFile.dll property1
echo REMOVING property1
size=$(wc -c < "$file")
if [ $size -eq $new_size ]; then
    echo Good
else
    echo Wrong
fi
