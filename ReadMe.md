# RENToday

_Rename a file/all files in a folder to today's date (and time)_

---

## Usage

`RENToday /f=<Filename>|/d=<directory with file spec> [/p=<Prefix>] [/o] [/s]`

e.g.

- `RENToday /f=d:\data\myfile.txt`  
Will rename the single file d:\data\myfile.txt to 20020228_134228_623.txt, assuming today's 
date is _February 28th, 2002_ and the time is _13:42:28_ (and 623 milliseconds).

   or  

- `RENToday /d=d:\data\*.txt`  
Will rename each file with the file extension .txt in d:\data\ to 20020228_134228_623.txt, assuming today's date is _February 28th, 2002_ and the time is _13:42:28_ (and 623 milliseconds) when the first renaming occurs.  
Of course, the time will be updated for each following renaming process. RENToday implements a 3 millisecond delay internally to ensure that file names are unique.

__Please note__: switch `/f` takes precedence over switch `/d`.

- `RENToday /f=d:\data\myfile.txt /p=MyPrefix_`  
Will rename d:\data\myfile.txt to MyPrefix_20020228_134228_623.txt, assuming the above example's date & time.

Option `/p=<prefix>` adds `<prefix>` in front of the file's name. If `<prefix>` includes the character '*', the file' 
original name will be placed at that position, e.g. `RENToday /f=d:\data\myfile.txt /p=MyPrefix_*_` results in MyPrefix_myfile_20020228_134228_623.txt

Option `/o` will overwrite an(y) existing file(s).
Option `/s` will recurse subdirectories when used in conjunction with `/s` and ignored otherwise.

## Dependencies

- [libBAUtilCSCore](https://github.com/knuth-konrad/libBAUtilCSCore)
