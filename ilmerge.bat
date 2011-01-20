mkdir output\lib
cd src\Cashbox\bin\Release
..\..\..\..\lib\ILMerge\ILMerge.exe/internalize /target:dll /out:..\Cashbox.dll /ndebug /allowDup Cashbox.dll Magnum.dll Stact.dll System.Threading.dll
move /y ..\Cashbox.dll ..\..\..\..\output\lib
cd ..\..\..\..