#!/bin/bash
file=testFile.dll
cp PropertiesFileEditor.dll testFile.dll
./PropertiesFileEditor.exe remove testFile.dll property1
echo REMOVE WITHOUT FOOTER
DIFF=$(diff PropertiesFileEditor.dll testFile.dll)
if [ "$DIFF" == "" ]; then
	echo Good
else
	echo Wrong
fi
./PropertiesFileEditor.exe edit testFile.dll property1=value1
echo EDIT WITHOUT FOOTER
DIFF=$(diff PropertiesFileEditor.dll testFile.dll)
if [ "$DIFF" == "" ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe add testFile.dll property1=value1
echo ADDING property1=value1 WITHOUT FOOTER
size=$(wc -c < "$file")
if [ $size -eq 9254 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe edit testFile.dll property1=v1
echo EDITTING property1=v1 
size=$(wc -c < "$file")
if [ $size -eq 9250 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe remove testFile.dll property1
echo REMOVING property1
size=$(wc -c < "$file")
if [ $size -eq 9236 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe add testFile.dll property1=value1
echo ADDING property1=value1
size=$(wc -c < "$file")
if [ $size -eq 9254 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe add testFile.dll property2=value2
echo ADDING property2=value2
size=$(wc -c < "$file")
if [ $size -eq 9272 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe remove testFile.dll property3
echo REMOVING NON-EXISTING PROP
size=$(wc -c < "$file")
if [ $size -eq 9272 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe edit testFile.dll property3=value3
echo EDITING NON-EXISTING PROP
size=$(wc -c < "$file")
if [ $size -eq 9272 ]; then
	echo Good
else
	echo Wrong
fi


./PropertiesFileEditor.exe edit testFile.dll property2=newvalue2
echo EDITING property2=newvalue2
size=$(wc -c < "$file")
if [ $size -eq 9275 ]; then
	echo Good
else
	echo Wrong
fi

./PropertiesFileEditor.exe remove testFile.dll property1
echo REMOVING property1
size=$(wc -c < "$file")
if [ $size -eq 9257 ]; then
	echo Good
else
	echo Wrong
fi


