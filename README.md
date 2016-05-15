Tokenizer
==============

[![Build status](https://ci.appveyor.com/api/projects/status/ifrc0whlp4cbeihv?svg=true)](https://ci.appveyor.com/project/PiotrOwsiak/tokenizer)

This program generates a file using a template with tokens that will be replaced using token file (with token definitions).
Usefull for generating configuration files for different environments.
Also the tool can detect changes to generated file (saves a copy as *.lastversion) and can open a diff tool automatically.
The default difftool is WinMerge (with hardcoded path) but you can specify your own using DIFFTOOL and DIFFTOOLCALLPATTERN environment variables.
Just see the code to understand how it works.

###Usage:  

Tokenizer.exe template-file token-file output-file

###Warning:  
This program has been done very quickly and "works on my machine" (Windows 7 EN 64-bit and Windows 7 PL 64-bit). It has been tested only on a few machines.

Pull requests are welcomed.

###Known problems:  

###TODO:

###Credits
